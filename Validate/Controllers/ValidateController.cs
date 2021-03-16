using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace validate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidateController : ControllerBase
    {
        static MySqlConnection connection;
        const string HEAD = "Head";
        const string DEPUTY_HEAD = "Deputy Head";
        const string MANAGER = "Manager";
        const string SENIOR_MEMBER = "Senior member";
        const float ON_DUTY_PERCENTAGE_RELAX = 40;
        const float ON_DUTY_PERCENTAGE_NORMAL = 60;
        const float ON_DUTY_PERCENTAGE_PEEK = 80;

        private void OpenDatabaseConnection()
        {
            try
            {
                connection = new MySqlConnection
                {
                    ConnectionString = "server=localhost;port=3306;user=root;database=LeaveManagement"
                };
                connection.Open();
                Console.WriteLine("Connected to Database");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error : " + e.ToString());
            }
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<ValidateController> _logger;

        public ValidateController(ILogger<ValidateController> logger)
        {
            _logger = logger;
        }

        [HttpGet("empId={empId}&startDate={startDate}&endDate={endDate}")]
        public KeyValuePair<int, string>[] ValidateHolydayRequest(int empId, string startDate, string endDate)
        {
            var rng = new Random();
            Dictionary<int, string> validations = new Dictionary<int, string>();
            OpenDatabaseConnection();

            DateTime startDateTime = DateTime.Parse(startDate);

            // not between 23/12 to 03/01
            validateRemainingLeaves(validations, empId);
            validateHeadStaff(validations, empId, startDateTime);
            validateSeniorStaff(validations, empId, startDateTime);
            validateOnDutyStaff(validations, empId, startDateTime, ON_DUTY_PERCENTAGE_NORMAL);

            // between 23/12 to 03/01

            connection.Close();
            return validations.ToArray();
        }

        private void validateRemainingLeaves(Dictionary<int, string> validations, int empId)
        {
            MySqlCommand command = new MySqlCommand();
            MySqlDataReader dataReader;
          
            command.Connection = connection;
            command.CommandText = "SELECT GetRemainingLeavesCount(@empId) RemainingLeaveCount";
            command.Parameters.AddWithValue("@empId", empId);

            dataReader = command.ExecuteReader();

            if (dataReader.Read())
            {
                int remainingLeaveCount = dataReader.GetInt16("RemainingLeaveCount");
                if (remainingLeaveCount == 0)
                {
                    validations[1] = "No Remaining Leaves";
                }
            }
            dataReader.Close();
        }

        private void validateHeadStaff(Dictionary<int, string> validations, int empId, DateTime startDateTime)
        {
            string empRole = getRoleName(empId);

            if (empRole.Equals(HEAD) || empRole.Equals(DEPUTY_HEAD))
            {
                int headStaffCount = getLeaveStaffCount(empId, startDateTime, HEAD);
                if (headStaffCount > 0)
                {
                    validations[3] = "No Head Staff on Duty";
                }

                int deputyHeadStaffCount = getLeaveStaffCount(empId, startDateTime, DEPUTY_HEAD);
                if (deputyHeadStaffCount > 0)
                {
                    validations[2] = "No Duputy Head Staff on Duty";
                }
            }
        }

        private string getRoleName(int empId)
        {
            MySqlCommand command = new MySqlCommand();
            MySqlDataReader dataReader;

            command.Connection = connection;
            command.CommandText = "SELECT GetRoleName(@empId) roleName";
            command.Parameters.AddWithValue("@empId", empId);

            dataReader = command.ExecuteReader();
            string roleName = "";

            if (dataReader.Read())
            {
                roleName = dataReader.GetString("roleName");
            }
            dataReader.Close();
            return roleName;
        }

        private int getLeaveStaffCount(int empId, DateTime startDateTime, string roleName)
        {
            MySqlCommand command = new MySqlCommand();
            MySqlDataReader dataReader;

            command.Connection = connection;
            command.CommandText = "SELECT GetLeaveStaffCount(@empId, @startDateTime, @roleName) staffCount";
            command.Parameters.AddWithValue("@empId", empId);
            command.Parameters.AddWithValue("@startDateTime", startDateTime);
            command.Parameters.AddWithValue("@roleName", roleName);

            dataReader = command.ExecuteReader();
            int staffCount = 0;

            if (dataReader.Read())
            {
                staffCount = dataReader.GetInt16("staffCount");
            }
            dataReader.Close();
            return staffCount;
        }

        private void validateSeniorStaff(Dictionary<int, string> validations, int empId, DateTime startDateTime)
        {
            int managerCount = getTotalStaffCount(empId, MANAGER);
            int managerLeaveCount = getLeaveStaffCount(empId, startDateTime, MANAGER);
            if (managerCount - managerLeaveCount == 0)
            {
                validations[4] = "No Manager Staff on Duty";
            }

            int seniorSatffCount = getTotalStaffCount(empId, SENIOR_MEMBER);
            int seniorSatffLeaveCount = getLeaveStaffCount(empId, startDateTime, SENIOR_MEMBER);
            if (seniorSatffCount - seniorSatffLeaveCount == 0)
            {
                validations[5] = "No Senior Satff on Duty";
            }
        }

        private int getTotalStaffCount(int empId, string roleName)
        {
            MySqlCommand command = new MySqlCommand();
            MySqlDataReader dataReader;
            int staffCount = 0;

            command.Connection = connection;
            command.CommandText = "SELECT GetTotalStaffCount(@empId, @roleName) staffCount";
            command.Parameters.AddWithValue("@empId", empId);
            command.Parameters.AddWithValue("@roleName", roleName);

            dataReader = command.ExecuteReader();

            if (dataReader.Read())
            {
                staffCount = dataReader.GetInt16("staffCount");
            }
            dataReader.Close();
            return staffCount;
        }

        private void validateOnDutyStaff(Dictionary<int, string> validations, int empId, DateTime startDateTime, float percentage)
        {
            int totalStaffCount = getTotalStaffCount(empId, "");
            int leaveStaffCount = getLeaveStaffCount(empId, startDateTime, "");
            if (totalStaffCount != 0)
            {
                if ((totalStaffCount - leaveStaffCount) * 100 / totalStaffCount < percentage)
                {
                    validations[6] = "No Minimum Satff on Duty";
                }
            }
            //TODO: 60%
            // in august 40%
        }
    }
}
