using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConflictManagementLibrary.Model.Trip
{
    public class TrainTypeDetails
    {
        public int ID { get; set; }
        public string? Description { get; set; }
        public int DefaultLength { get; set; }
        public uint DerivedFrom { get; set; }
        public string? ColorLine { get; set; }
        public string? ColorRoutedLine { get; set; }
        public string? ColorStop { get; set; }
        public bool CanBeConsist { get; set; }
        public bool UseAsVehicle { get; set; }
        public bool CanBeChild { get; set; }
        public string? UIImage { get; set; }

        public TrainTypeDetails()
        {
            
        }
    }
}
