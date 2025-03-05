using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverificationWorkerDemo.Models
{
    namespace MCSVC.Models
    {
        public class RiskRatingConfig
        {
            public int RiskRatingID { get; set; }
            public string RiskRating { get; set; } = string.Empty;
            public int RevPeriodMonths { get; set; }
            public DateTime LastUpdated { get; set; } = DateTime.Now;
            public bool IsActive { get; set; }
        }

        public class FatcaConfig
        {
            public int ConfigID { get; set; }
            public int RevPeriodMonths { get; set; }
            public DateTime LastUpdated { get; set; } = DateTime.Now;
            public bool IsActive { get; set; }
        }

        public class Customer
        {
            public Guid CustomerID { get; set; }
            public string? DigitalID { get; set; }
            public DateTime? FatcaLastRevDate { get; set; }
            public string RIM_No { get; set; } 
            public int? NotificationCounter { get; set; } = 0;
            public DateTime? ReverificationDueDate { get; set; }
            public DateTime? FatcaDueDate { get; set; }
            public DateTime? NextNotificationDate { get; set; }
            public DateTime? OnboardingDate { get; set; }
            public string? RiskRating { get; set; }
            public DateTime? DateCreated { get; set; } = DateTime.Now;
            public DateTime? DateUpdated { get; set; }
            public DateTime? LastRevDate { get; set; }
            public DateTime? LastFatcaDate { get; set; }
            public bool? IsMandatoryRevScreen { get; set; } = false;
            public bool? IsLocked { get; set; } = true;
        }

        public class CustomerAccountActionsHistory
        {
            public int ActionID { get; set; }
            public Guid CustomerID { get; set; }
            public string ActionType { get; set; } = string.Empty;
            public DateTime ActionDate { get; set; } = DateTime.Now;
            public string? ActionReason { get; set; }
            public string? PerformedBy { get; set; }
            public string? Status { get; set; }
        }

        public class NotificationHistory
        {
            public int HistoryID { get; set; }
            public Guid CustomerID { get; set; }
            public string NotificationType { get; set; } = string.Empty;
            public string? Message { get; set; }
            public string? Status { get; set; }
            public DateTime SentDate { get; set; }
        }
    }
}
