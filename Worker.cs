using System.Data;
using Microsoft.Data.SqlClient;
using ReverificationWorkerDemo.Models.MCSVC.Models;

namespace ReverificationWorkerDemo
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    //await GetCustomersDueForRev();
                    await RestrictCustomerAndNotifyAsync();
                }
                await Task.Delay(1000000, stoppingToken);
            }
        }

        public async Task<List<Customer>> GetCustomersDueForRev()
        {
            List<Customer> customers = new List<Customer>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("ReverificationDbConnection")!))
                {
                    using (SqlCommand cmd = new SqlCommand("MCSVC.GetCustomersDueForReverification", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Risk_Rating", "No Risk");
                        cmd.Parameters.AddWithValue("@isReverification", 1);
                        cmd.Parameters.AddWithValue("@isFATCA", 1);
                        cmd.Parameters.AddWithValue("@ReverificationInterval", 1);

                        await connection.OpenAsync();
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                customers.Add(new Customer
                                {
                                    RIM_No = rdr.GetString(rdr.GetOrdinal("RIM_No")),
                                    DigitalID = rdr["DigitalID"] as string,
                                    NotificationCounter = rdr["NotificationCounter"] as int?,
                                    ReverificationDueDate = rdr["ReverificationDueDate"] as DateTime?,
                                    FatcaDueDate = rdr["FatcaDueDate"] as DateTime?,
                                    RiskRating = rdr.GetString(rdr.GetOrdinal("RiskRating")),
                                    IsLocked = rdr.GetBoolean(rdr.GetOrdinal("IsLocked")),
                                    IsMandatoryRevScreen = rdr.GetBoolean(rdr.GetOrdinal("IsMandatoryRevScreen")),
                                    OnboardingDate = rdr.GetDateTime(rdr.GetOrdinal("OnboardingDate"))
                                });
                            }

                             DisplayCustomers(customers);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching customers: {ex.Message}");
            }

            return customers;
        }

        public void DisplayCustomers(List<Customer> revCustomers)
        {
            if (revCustomers == null || revCustomers.Count == 0)
            {
                Console.WriteLine("No customers due for reverification.");
                return;
            }
            Console.WriteLine("===============================================");
            Console.WriteLine("Customer Information");
            Console.WriteLine("===============================================");
            int index = 1;

            foreach (var customer in revCustomers)
            {
                Console.WriteLine($"#{index++}:");
                Console.WriteLine($"  RIM No: {customer.RIM_No}");
                Console.WriteLine($"  Digital ID: {customer.DigitalID}");
                Console.WriteLine($"  Notification Counter: {customer.NotificationCounter}");
                Console.WriteLine($"  Reverification Due Date: {customer.ReverificationDueDate:yyyy-MM-dd}");
                Console.WriteLine($"  FATCA Due Date: {customer.FatcaDueDate:yyyy-MM-dd}");
                Console.WriteLine($"  Risk Rating: {customer.RiskRating}");
                Console.WriteLine($"  Is Locked: {customer.IsLocked}");
                Console.WriteLine($"  Is Mandatory Review Screen: {customer.IsMandatoryRevScreen}");
                Console.WriteLine($"  Onboarding Date: {customer.OnboardingDate:yyyy-MM-dd}");
                Console.WriteLine("-----------------------------------------------");
            }

            Console.WriteLine("===============================================");
        }

        public async Task<int> RestrictCustomerAndNotifyAsync()
        {
            List<Customer> customers = new List<Customer>
    {
        new Customer
        {
            RIM_No = "15992111",
            FatcaLastRevDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            NotificationCounter = 0,
            ReverificationDueDate = null,
            FatcaDueDate = null,
            OnboardingDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            RiskRating = "High",
            DateUpdated = DateTime.Now,
            LastRevDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            LastFatcaDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            IsMandatoryRevScreen = false,
            IsLocked = false
        }
        ,
        new Customer
        {
            RIM_No = "15992100        ",
            FatcaLastRevDate = DateTime.Parse("2025-03-05 00:00:00.000"),
            NotificationCounter = 1,
            ReverificationDueDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            FatcaDueDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            OnboardingDate = DateTime.Parse("2025-03-05 00:00:00.000"),
            RiskRating = "High",
            DateUpdated = DateTime.Now,
            LastRevDate = DateTime.Parse("2025-03-04 00:00:00.000"),
            LastFatcaDate = DateTime.Parse("2025-03-05 00:00:00.000"),
            IsMandatoryRevScreen = true,
            IsLocked = true
        }
    };

            int affectedRows = 0;

            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("ReverificationDbConnection")!))
                {
                    await connection.OpenAsync();

                    foreach (var customer in customers)
                    {
                        using (SqlCommand cmd = new SqlCommand("MCSVC.UpdateOrInsertCustomerRevInfo", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Rim_No", customer.RIM_No);
                            cmd.Parameters.AddWithValue("@FatcaLastRevDate", customer.FatcaLastRevDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NotificationCounter", customer.NotificationCounter ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ReverificationDueDate", customer.ReverificationDueDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@FatcaDueDate", customer.FatcaDueDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@OnboardingDate", customer.OnboardingDate);
                            cmd.Parameters.AddWithValue("@RiskRating", customer.RiskRating);
                            cmd.Parameters.AddWithValue("@DateUpdated", customer.DateUpdated ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LastRevDate", customer.LastRevDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LastFatcaDate", customer.LastFatcaDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@isMandatoryRevScreen", customer.IsMandatoryRevScreen);
                            cmd.Parameters.AddWithValue("@isLocked", customer.IsLocked);

                            affectedRows += await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                Console.WriteLine($"Successfully processed {affectedRows} customers.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating customers: {ex.Message}");
            }

            return affectedRows; 
        }
    }
}
