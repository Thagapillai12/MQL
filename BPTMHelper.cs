using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.bt.util.logging;
using System.Configuration;

namespace BT.SaaS.MSEOAdapter
{
    public class BPTMHelper
    {
        private const int BPTMTxnNumber = 10;
        private const string BPTMTxnString = "E2E.busTxnSeq";


        /// <summary>
        /// Method used to fetch BPTM Transaction Id from BPTM E2e Data string
        /// <para>Note: This internally will not invoke any BPTM library. Its just using string handling functions.</para>
        /// </summary>
        /// <param name="e2eDataString">BPTM e2e Data string</param>
        /// <returns>E2e Transacation Id</returns>
        public static string GetTxnIdFromE2eData(string e2eDataString)
        {
            try
            {
                if (!string.IsNullOrEmpty(e2eDataString))
                {
                    var e2eDataSplit = e2eDataString.Split(',');

                    foreach (var bptmVal in e2eDataSplit)
                    {
                        if (bptmVal.StartsWith(string.Concat(BPTMTxnNumber, '=')) || bptmVal.StartsWith(string.Concat(BPTMTxnString, '=')))
                        {
                            return bptmVal.Split('=').Last();
                        }
                    }
                }
                return "BPTME2eTxnId";
            }
            catch(Exception ex)
            {
                return "BPTME2eTxnId";
            }
        }
    }
}
