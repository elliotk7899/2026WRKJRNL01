using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using MudBlazor;
using PhoneNumbers;

namespace _2026WRKJRNL01.UI
{
    public static class StringExtensions
    {
        static public string FormatUrlWithHttps(this string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            url = url.Trim();

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url.Substring("http://".Length);
            }

            return url;
        }

        public static bool IsEmailValid(this string email)
        {
            try
            {
                email = email.Trim();
                MailAddress mail = new MailAddress(email);

                //check for domain extension since RFC rules are garbage and allow emails like user@domain
                //return true if the email is valid and has a proper domain extension
                return mail.Address == email &&
                   mail.Host.Contains('.') &&
                   !mail.Host.StartsWith('.') &&
                   !mail.Host.EndsWith('.');

            }
            catch (FormatException)
            {
                return false; // Invalid email format
            }
        }

        public static bool IsPhoneValid(this string phone)
        {
            PhoneNumberUtil util = PhoneNumberUtil.GetInstance();
            try
            {
                PhoneNumber number = util.Parse(phone, "US");

                if (util.IsValidNumber(number))
                {
                    return true;
                }

                return false;
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        public static string AddSpacesToWords(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var result = new System.Text.StringBuilder();
            result.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }

            return result.ToString();
        }

        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Replace non-letter/digit characters with space
            var cleaned = Regex.Replace(input, @"[^a-zA-Z0-9]", " ");

            // Convert to title case (each word capitalized)
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            string titled = textInfo.ToTitleCase(cleaned.ToLower());

            // Normalize multiple spaces to a single space
            return Regex.Replace(titled, @"\s+", " ");
        }

        public static string FormatPhoneNumber(this string value)
        {
            PhoneNumbers.PhoneNumberUtil util = PhoneNumbers.PhoneNumberUtil.GetInstance();

            PhoneNumbers.PhoneNumber number;

            try
            {
                number = util.Parse(value, "US");
            }
            catch
            {
                return string.Empty;
            }

            if (util.IsValidNumber(number))
            {
                return util.Format(number, PhoneNumbers.PhoneNumberFormat.NATIONAL);
            }

            return string.Empty;
        }

        public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }
    }

    public static class MudExtensions
    {
        public static RenderFragment RenderEmailLink(string email, string style = "font-size:var(--mud-typography-default-size)") => builder =>
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            builder.OpenComponent<MudLink>(0);
            builder.AddAttribute(1, "Href", $"mailto:{email}");

            if (!string.IsNullOrEmpty(style))
            {
                builder.AddAttribute(1, "style", $"{style}");
            }

            builder.AddAttribute(2, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.AddContent(3, email);
            }));
            builder.AddAttribute(1, "target", "_blank");
            builder.CloseComponent();
        };
    }

    public static class DialogServiceExtensions
    {
        public static async Task<bool> ConfirmAsync(
            this IDialogService dialogService,
            string title,
            string message,
            string confirmText = "Confirm",
            string cancelText = "Cancel",
            Color confirmColor = Color.Success,
            Color cancelColor = Color.Dark)
        {
            var parameters = new DialogParameters<MudMessageBox>
        {
            { x => x.Title, title },
            { x => x.MarkupMessage, (MarkupString)message },
            { x => x.YesButton, CreateButton(confirmText, confirmColor) },
            { x => x.CancelButton, CreateButton(cancelText, cancelColor) }
        };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true
            };

            var dialog = await dialogService.ShowAsync<MudMessageBox>(
                title,
                parameters,
                options);

            var result = await dialog.Result;

            return result is not null && !result.Canceled;
        }

        private static RenderFragment CreateButton(string text, Color color) =>
            builder =>
            {
                builder.OpenComponent<MudButton>(0);
                builder.AddAttribute(1, nameof(MudButton.Color), color);
                builder.AddAttribute(2, nameof(MudButton.Variant), Variant.Filled);
                builder.AddAttribute(
                    3,
                    nameof(MudButton.ChildContent),
                    (RenderFragment)(b => b.AddContent(0, text)));
                builder.CloseComponent();
            };
    }
}