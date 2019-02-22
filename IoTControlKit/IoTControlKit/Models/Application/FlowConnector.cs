using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Application
{
    [NPoco.TableName("FlowConnector")]
    public class FlowConnector : BasePoco
    {
        public string Guid { get; set; }
        public long TargetFlowComponentd { get; set; } //to
        public long SourceFlowComponentd { get; set; } //from
        public string SourcePort { get; set; } //true, false, <, <=, = etc.
    }
}
