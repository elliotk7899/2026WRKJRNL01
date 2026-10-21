using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using WRKJRNL01.Data.Repository;

namespace WRKJRNL01.Data.Services;

public class GoogleCalendarService
{
    private readonly IConfiguration configuration;
    private readonly TokenEncryptionService tokenEncryptionService;
    private readonly GoogleConnectRepo googleConnectRepo;

    public GoogleCalendarService(
        IConfiguration configuration,
        TokenEncryptionService tokenEncryptionService,
        GoogleConnectRepo googleConnectRepo)
    {
        this.configuration = configuration;
        this.tokenEncryptionService = tokenEncryptionService;
        this.googleConnectRepo = googleConnectRepo;
    }

    // OFFLINE STUB: Google Calendar integration is disabled for local/off-network use.
    // The UI/controls remain, but no Google API calls are made. Flip this to false only
    // if you wire real Google credentials back up.
    private const bool GoogleDisabledLocally = true;

    //get events for a specific date that the user passes in
    public async Task<IList<Event>> GetUpcomingEventsAsync(string userEmail, DateTime date)
    {
        // OFFLINE: never contact Google; report no events.
        if (GoogleDisabledLocally)
            return new List<Event>();

        var connection = await googleConnectRepo.GetByEmailAsync(userEmail);

        if (connection == null ||
            !connection.GoogleConnected ||
            string.IsNullOrWhiteSpace(
                connection.GoogleRefreshToken))
        {
            return new List<Event>();
        }

        var accessToken = await GetAccessTokenAsync(connection.GoogleRefreshToken);

        var credential = GoogleCredential.FromAccessToken(accessToken);

        var service = new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "WRKJRNL01"
            });

        var request = service.Events.List("primary");

        var startOfDay = date.Date;

        var endOfDay = startOfDay.AddDays(1);

        request.TimeMinDateTimeOffset = startOfDay;

        request.TimeMaxDateTimeOffset = endOfDay;

        request.SingleEvents = true;

        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();

        //var blob = System.Text.Json.JsonSerializer.Serialize(events);
        //Console.WriteLine($"--------------Events for {date.Date}-------------------");
        //Console.WriteLine(blob);

        var acceptedEvents = events.Items?
    .Where(e =>

        // Accepted invitation
        (e.Attendees != null &&
         e.Attendees.Any(a =>
             a.Self == true &&
             a.ResponseStatus == "accepted"))

        // Working location
        || (e.Attendees == null &&
            e.EventType == "workingLocation")

        // Personal event created by me with no attendees
        || (e.Organizer?.Self == true)
        )
        .ToList() ?? new List<Event>();

        return acceptedEvents;
    }

    private async Task<string> GetAccessTokenAsync(string encryptedRefreshToken)
    {
        var refreshToken = tokenEncryptionService.Decrypt(encryptedRefreshToken);

        var flow =
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets =
                        new ClientSecrets
                        {
                            ClientId =
                                configuration["Google:ClientId"],

                            ClientSecret =
                                configuration["Google:ClientSecret"]
                        }
                });

        var response = await flow.RefreshTokenAsync("user", refreshToken, CancellationToken.None);

        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new Exception("Google did not return an access token.");
        }

        return response.AccessToken;
    }

    public async Task<bool> IsConnectedAsync(string userEmail)
    {
        // OFFLINE: always report "not connected" so the UI shows the connect control
        // without attempting any Google calls.
        if (GoogleDisabledLocally)
            return false;

        var connection = await googleConnectRepo.GetByEmailAsync(userEmail);

        if (connection is not { GoogleConnected: true } || string.IsNullOrWhiteSpace(connection.GoogleRefreshToken))
            return false;

        try
        {
            var accessToken = await GetAccessTokenAsync(connection.GoogleRefreshToken);

            return !string.IsNullOrWhiteSpace(accessToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking Google connection for {userEmail}: {ex}");

            //check to see if the user exists in the dbo.GoogleCalendarConnections table and clear the refresh token if exists
            await googleConnectRepo.DisconnectAsync(userEmail);

            return false;
        }
    }

    public async Task<Dictionary<string, bool>> GetCalendarSettings(string userEmail)
    {
        // OFFLINE: no settings since there is no Google connection locally.
        if (GoogleDisabledLocally)
            return new Dictionary<string, bool>();

        var connection = await googleConnectRepo.GetByEmailAsync(userEmail);

        if (connection is not { GoogleConnected: true } || string.IsNullOrWhiteSpace(connection.GoogleRefreshToken))
            return new Dictionary<string, bool>();

        var results = new Dictionary<string, bool>
        {
            { "AutoImportEvents", connection.AutoImportEvents },
        };

        return results;
    }

    public async Task<bool> IsAbsentAsync(string userEmail, DateTime date)
    {
        // OFFLINE: service-account calendar lookups are disabled locally.
        if (GoogleDisabledLocally)
            return false;

        if (string.IsNullOrWhiteSpace(userEmail))
            return false;

        if (date.DayOfWeek == DayOfWeek.Saturday ||
            date.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        var serviceAccountJson =
            configuration["Google:ServiceAccountJson"];

        var calendarId =
            configuration["Google:CalendarId"];

        if (string.IsNullOrWhiteSpace(serviceAccountJson) ||
            string.IsNullOrWhiteSpace(calendarId))
        {
            return false;
        }

#pragma warning disable CS0618
        var credential = GoogleCredential
            .FromJson(serviceAccountJson)
            .CreateScoped(CalendarService.Scope.CalendarReadonly);
#pragma warning restore CS0618

        var service = new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = configuration["AppID"] ?? "WRKJRNL01"
            });

        var request = service.Events.List(calendarId);

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        request.TimeMinDateTimeOffset = startOfDay;
        request.TimeMaxDateTimeOffset = endOfDay;
        request.SingleEvents = true;
        request.OrderBy =
            EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();
        foreach (var ev in events.Items ?? new List<Event>())
        {
            Console.WriteLine(
                $"Absence Event: {ev.Summary} | " +
                $"Creator: {ev.Creator?.Email} | " +
                $"Start: {ev.Start?.Date}");
        }
        return events.Items?.Any(e =>
            e.Start?.Date != null &&
            IsAbsenceEvent(e.Summary) &&
            string.Equals(
                e.Creator?.Email,
                userEmail,
                StringComparison.OrdinalIgnoreCase))
            ?? false;
    }

    private static bool IsAbsenceEvent(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return false;

        var normalized = summary
            .Trim()
            .ToLowerInvariant();

        var words = normalized
            .Replace("-", " ")
            .Split(
                ' ',
                StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries);

        // Full absence labels can appear anywhere.
        foreach (var word in words)
        {
            switch (word)
            {
                case "personal":
                case "sick":
                case "annual":
                case "out":
                    return true;
            }
        }

        // Single-letter codes
        var parts = normalized.Split('-',
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            return false;

        var status = parts[^1];

        return status switch
        {
            "p" => true,
            "s" => true,
            "a" => true,
            _ => false
        };
    }

    public async Task<HashSet<string>> GetAbsentEmployeeEmailsAsync(DateTime date)
    {
        // OFFLINE: service-account calendar lookups are disabled locally.
        if (GoogleDisabledLocally)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (date.DayOfWeek == DayOfWeek.Saturday ||
            date.DayOfWeek == DayOfWeek.Sunday)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var serviceAccountJson = configuration["Google:ServiceAccountJson"];
        var calendarId = configuration["Google:CalendarId"];

        if (string.IsNullOrWhiteSpace(serviceAccountJson) ||
            string.IsNullOrWhiteSpace(calendarId))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

#pragma warning disable CS0618
        var credential = GoogleCredential
            .FromJson(serviceAccountJson)
            .CreateScoped(CalendarService.Scope.CalendarReadonly);
#pragma warning restore CS0618

        var service = new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = configuration["AppID"] ?? "WRKJRNL01"
            });

        var request = service.Events.List(calendarId);

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        request.TimeMinDateTimeOffset = startOfDay;
        request.TimeMaxDateTimeOffset = endOfDay;
        request.SingleEvents = true;
        request.OrderBy =
            EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();

        return events.Items?
            .Where(e =>
                e.Start?.Date != null &&
                IsAbsenceEvent(e.Summary) &&
                !string.IsNullOrWhiteSpace(e.Creator?.Email))
            .Select(e => e.Creator!.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}