using System;

namespace COA.SettlementFix
{
    public class Creditor
    {
        public int CreditorId { get; set; }
        public int SettlementAttemptId { get; set; }
        public DateTime ResponseDate { get; set; }
        public int RejectReasonId { get; set; }
        public string RejectReasonComment { get; set; }
        public string AttemptedStatus { get; set; }
        public DateTime AttemptCreated { get; set; }
        public string CurrentStatus { get; set; }
        public string ClientName { get; set; }
        public string AccountNumber { get; set; }
        public decimal CurrentBalance { get; set; }
        public string CreditorName { get; set; }
    }
}
