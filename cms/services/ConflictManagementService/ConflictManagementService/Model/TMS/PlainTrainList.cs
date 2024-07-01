using System.Collections.Generic;
using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlRoot("PlainTrainList")]
    public class PlainTrainList
    {
        public PlainTrainList()
        {
        }
        [XmlArray("Trains")]
        public List<ATRPlainTrain> trains = new();
    }
}
