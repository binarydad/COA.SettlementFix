using COA.Common;
using COA.EnterpriseServices.Creditors;
using COA.SettlementFix.CreditorServiceWcf;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace COA.SettlementFix
{
    class Program
    {
        static void Main(string[] args)
        {
            var settingsProxy = new SettingsProxy();
            var quickbaseConnectionString = settingsProxy.GetConnectionString(SettingsProxy.Databases.Quickbase);

			var creditorsToFix = DBWrapper.ExecuteCommand(quickbaseConnectionString, cmd =>
			{
				cmd.CommandText = @"select 
					responseStatus.CreditorID
					, responseStatus.ResponseDate
					, responseStatus.RejectReasonID
					, responseStatus.RejectReasonComment
					, responseStatus.[Status Name] AttemptedStatus
					, sa.[Date Created] AttemptCreated
					, currentStatus.[**Creditor Status] CurrentStatus
					, cl.[Enrolled Client Name] ClientName
					, cr.[Account #] AccountNumber
					, cr.[Current Balance] CurrentBalance 
					, pro.[Creditor Name] CreditorName
					, sa.[Record ID#] SettlementAttemptId
				from
				(
					select cr.CreditorID, st.[Status Name], cr.ResponseDate, cr.RejectReasonID, cr.RejectReasonComment, row_number() over (partition by cr.CreditorID order by cr.ResponseDate desc) RowNum
					from CreditorResponse cr
					join QB_Creditor_Status_Types st on st.[Record ID#] = cr.CreditorStatusTypeID	
					where cr.ResponseDate > '9/16/2020'
				) responseStatus
				cross apply dbo.[udfQB_Creditor_**CreditorStatus](responseStatus.CreditorID) currentStatus
				join QB_Creditors cr (nolock) on cr.[Record ID#] = responseStatus.CreditorID
				join QB_COA_Creditor pro (nolock) on pro.[Record ID#] = cr.[Related Current Creditor Primary]
				join QB_Client cl (nolock) on cl.[Record ID#] = cr.[Related Client]
				join QB_Settlement_Attempt sa (nolock) on sa.[Record ID#] = currentStatus.[Max Settlement Attempt Record ID]
				where responseStatus.RowNum = 1
				and currentStatus.[**Creditor Status] in ('Offer Made Pending', 'Waiting for SIF')
				and responseStatus.ResponseDate > sa.[Date Created]
				order by cl.[Enrolled Client Name]";

				return cmd.ToList<Creditor>();
			});

			var client = new ServiceClient();

			foreach (var creditor in creditorsToFix)
            {
				var attempt = new SettlementAttemptUpdate
				{
					SettlementAttemptId = creditor.SettlementAttemptId,
					CreditorStatusType = creditor.AttemptedStatus.ToEnum<CreditorStatusType>(),
					Notes = creditor.RejectReasonComment
				};

				var result = client.UpdateSettlementAttempt(attempt);
            }
        }
    }
}
