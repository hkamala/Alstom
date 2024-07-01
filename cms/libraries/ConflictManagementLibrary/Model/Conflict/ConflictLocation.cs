using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Conflict
{
    [Serializable]
    public static class ConflictLocations
    {
        public static List<Location> MyLocations = new List<Location>();

        public static void AddLocations()
        {
            MyLocations.Add(Location.CreateInstance("Zemitāni VO1", 0));
            MyLocations.Add(Location.CreateInstance("Zemitāni VO2", 1));
            MyLocations.Add(Location.CreateInstance("Brasa", 2));
            MyLocations.Add(Location.CreateInstance("Sarkgandaugava", 3));
            MyLocations.Add(Location.CreateInstance("Mangaļi", 4));
            MyLocations.Add(Location.CreateInstance("Ziemeļblāzma", 0));
            MyLocations.Add(Location.CreateInstance("Vecāki", 5));
            MyLocations.Add(Location.CreateInstance("Carnikava", 6));
            MyLocations.Add(Location.CreateInstance("Gauja", 7));
            MyLocations.Add(Location.CreateInstance("Lilaste", 8));
            MyLocations.Add(Location.CreateInstance("Inčupe", 9));
            MyLocations.Add(Location.CreateInstance("Saulkrasti", 10));
            MyLocations.Add(Location.CreateInstance("Skulte", 11));
            //1.Zemitāni VO1
            //2.Zemitāni VO2
            //3.Brasa
            //4.Sarkgandaugava
            //5.Mangaļi
            //6.Ziemeļblāzma
            //7.Vecāki
            //8.Carnikava
            //9.Gauja
            //10.Lilaste
            //11.Inčupe
            //12.Saulkrasti
            //13.Skulte

        }

        public static Location GetLocation(int theIndex)
        {
            return MyLocations.FirstOrDefault(loc => loc.MyIndex == theIndex);
        }
        public static Location GetLocation(string theName)
        {
            return MyLocations.FirstOrDefault(loc => loc.MyName == theName);
        }

    }

    [Serializable]
    public class Location
    {
        //[JsonProperty(PropertyName = "MyName")]
        public string MyName { get; set; }

        //[JsonProperty(PropertyName = "MyIndex")]
        public int MyIndex { get; set; }

        public static Location CreateInstance(string theName, int theIndex)
        {
            return new Location(theName, theIndex);
        }

        [JsonConstructor]
        public Location()
        {
            
        }
        private Location(string theName, int theIndex)
        {
            MyName = theName;
            MyIndex = theIndex;
        }

    }
}
