using Microsoft.AspNetCore.Components;
using SDPBC.Employees.ServiceConnector;
using SDPBC.Employees.Types;

namespace _2026WRKJRNL01.UI.Components.Internal
{
    static public class DevTools
    {
        static public string GetVersion()
        {
            return typeof(DevTools).Assembly.GetName().Version?.ToString() ?? "unknown";
        }

        static public string GetFramework()
        {
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }

        static public bool IsDebug()
        {
            return Program.EnvironmentName?.ToLower() == "debug";
        }

        static public bool IsDev()
        {
            var env = Program.EnvironmentName?.ToLower();
            return env == "development";
        }

        static public bool IsTest()
        {
            var env = Program.EnvironmentName?.ToLower();
            return env == "testing";
        }

        static public bool IsProd()
        {
            var env = Program.EnvironmentName?.ToLower();
            return env == "production";
        }

        static public async Task<EmployeeVM> GetEmpFromEmpIdOrSAM(string empIdOrSAM, EmployeeServiceConnector employeeService)
        {
            try
            {
                if (employeeService == null)
                {
                    throw new InvalidOperationException("Employee Service Connector is not initialized.");
                }

                //get the employee by empID OR sAMAccountName
                var result = await employeeService.GetEmployee(empIdOrSAM) ?? await employeeService.GetEmployeeBysAMAccountName(empIdOrSAM);

                return result;
            }
            catch
            {
                return null;
            }
        }

        static public async Task<string> GetSAMAccountFromEmpId(string emplid, EmployeeServiceConnector employeeService)
        {
            var result = await GetEmpFromEmpIdOrSAM(emplid, employeeService);

            //switch to using the EmplId from the result
            emplid = result != null ? result.EmplId : emplid;

            return emplid;
        }

        static public async Task<string> GetDeptIdFromEmpId(string emplid, EmployeeServiceConnector employeeService)
        {
            var result = await GetEmpFromEmpIdOrSAM(emplid, employeeService);

            return result != null ? result.DeptId : string.Empty;
        }
    }
}
