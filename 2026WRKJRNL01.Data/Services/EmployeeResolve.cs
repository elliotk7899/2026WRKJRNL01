using SDPBC.Employees.ServiceConnector;
using SDPBC.Employees.Types;

namespace WRKJRNL01.Data.Services
{
    public class EmployeeResolve
    {
        private readonly EmployeeServiceConnector _employeeService;

        public EmployeeResolve(EmployeeServiceConnector employeeService)
        {
            _employeeService = employeeService;
        }

        public async Task<EmployeeVM?> GetEmployeeAsync(string empIdOrSam)
        {
            if (string.IsNullOrWhiteSpace(empIdOrSam))
                return null;

            try
            {
                var employee = await _employeeService.GetEmployee(empIdOrSam);

                if (employee is not null)
                    return employee;
            }
            catch
            {
                // Try SAM lookup next
            }

            try
            {
                return await _employeeService
                    .GetEmployeeBysAMAccountName(empIdOrSam);
            }
            catch
            {
                return null;
            }
        }
    }
}