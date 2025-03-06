using Grpc.Core;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ReverificationWorkerDemo.Protos
{
    public class ReverificationServiceHandler : ReverificationService.ReverificationServiceBase
    {
        private readonly ILogger<ReverificationServiceHandler> _logger;
        private readonly IConfiguration _configuration;

        public ReverificationServiceHandler(ILogger<ReverificationServiceHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override async Task<CustomerList> GetCustomersDueForRev(GetCustomersRequest request, ServerCallContext context)
        {
            var customers = new List<Customer>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("ReverificationDbConnection")!))
                {
                    using (SqlCommand cmd = new SqlCommand("MCSVC.GetCustomersDueForReverification", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Risk_Rating", request.RiskRating);
                        cmd.Parameters.AddWithValue("@isReverification", request.IsReverification);
                        cmd.Parameters.AddWithValue("@isFATCA", request.IsFatca);
                        cmd.Parameters.AddWithValue("@ReverificationInterval", request.ReverificationInterval);

                        await connection.OpenAsync();
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                customers.Add(new Customer
                                {
                                    DigitalId = rdr["DigitalID"].ToString(),
                                    RimNo = rdr["RIM_No"].ToString(),
                                    NotificationCounter = (int)(rdr["NotificationCounter"] as int?),
                                    ReverificationDueDate = rdr["ReverificationDueDate"]?.ToString(),
                                    FatcaDueDate = rdr["FatcaDueDate"]?.ToString(),
                                    RiskRating = rdr["RiskRating"].ToString(),
                                    IsLocked = rdr.GetBoolean(rdr.GetOrdinal("IsLocked")),
                                    IsMandatoryRevScreen = rdr.GetBoolean(rdr.GetOrdinal("IsMandatoryRevScreen")),
                                    OnboardingDate = rdr["OnboardingDate"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching customers: {ex.Message}");
            }

            return new CustomerList { Customers = { customers } };
        }

        public override async Task<UpsertResponse> UpsertCustomerRevInfo(CustomerList request, ServerCallContext context)
        {
            int affectedRows = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("ReverificationDbConnection")!))
                {
                    await connection.OpenAsync();
                    foreach (var customer in request.Customers)
                    {
                        using (SqlCommand cmd = new SqlCommand("MCSVC.UpdateOrInsertCustomerRevInfo", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@DigitalID", customer.DigitalId);
                            cmd.Parameters.AddWithValue("@Rim_No", customer.RimNo);
                            cmd.Parameters.AddWithValue("@FatcaLastRevDate", customer.FatcaLastRevDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@NotificationCounter",(object?)customer.NotificationCounter ?? DBNull.Value);


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
                return new UpsertResponse { AffectedRows = affectedRows, Message = "Successfully processed customers." };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating customers: {ex.Message}");
                return new UpsertResponse { AffectedRows = 0, Message = "Error processing customers." };
            }
        }
    }
}
