using System;
using System.Collections.Generic;
using System.IO;
using ConflictManagementLibrary.Model.Conflict;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Test
{
    public static class AddConflict
    {
        public static List<Conflict> MyConflictsCurrent = new List<Conflict>();
        public static List<Conflict> MyConflictsPast = new List<Conflict>();


        //public static void AddConflicts()
        //{
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(11, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "9876"), ConflictType.TypeOfConflict.Train, 0, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldTrain), DateTime.Now.AddMinutes(15)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(11, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "5678"), ConflictType.TypeOfConflict.Train, 1, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldTrain), DateTime.Now.AddMinutes(17)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(10, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "3456"), ConflictType.TypeOfConflict.Train, 2, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldOtherTrain), DateTime.Now.AddMinutes(19)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(9, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Station, "Track 12"), ConflictType.TypeOfConflict.Station, 3, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(21)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(8, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Station, "No Platform 99"), ConflictType.TypeOfConflict.Station, 4, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(23)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(7, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "Possession #4599"), ConflictType.TypeOfConflict.Track, 5, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(25)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(6, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "Blocked Track at M1"), ConflictType.TypeOfConflict.Track, 6, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(27)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(5, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "Non Control Track at T34"), ConflictType.TypeOfConflict.Track, 7, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(29)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(4, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "False Occupancy at T97"), ConflictType.TypeOfConflict.Track, 8, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(31)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(3, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "Non Control Switch at S54"), ConflictType.TypeOfConflict.Track, 9, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(33)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(2, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "False Occupancy at S12"), ConflictType.TypeOfConflict.Switch, 10, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(35)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(1, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "Switch Route Blocked Away From Route at S85"), ConflictType.TypeOfConflict.Switch,11, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(37)));
        //    MyConflictsCurrent.Add(Conflict.CreateInstance(0, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "Switch Route Blocked Away From Route at S39"), ConflictType.TypeOfConflict.Switch, 12, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(39)));

        //    MyConflictsPast.Add(Conflict.CreateInstance(11, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "9876"), ConflictType.TypeOfConflict.Train, 0, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldTrain), DateTime.Now.AddMinutes(-15)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(11, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "5678"), ConflictType.TypeOfConflict.Train, 1, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldTrain), DateTime.Now.AddMinutes(-17)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(10, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "3456"), ConflictType.TypeOfConflict.Train, 2, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldOtherTrain), DateTime.Now.AddMinutes(-19)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(9, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Station, "Track 12"), ConflictType.TypeOfConflict.Station, 3, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(-21)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(8, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Station, "No Platform 99"), ConflictType.TypeOfConflict.Station, 4, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(-23)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(7, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "Possession #4599"), ConflictType.TypeOfConflict.Track, 5, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(-25)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(6, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "Blocked Track at M1"), ConflictType.TypeOfConflict.Track, 6, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(-27)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(5, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "Non Control Track at T34"), ConflictType.TypeOfConflict.Track, 7, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(-29)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(4, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Track, "False Occupancy at T97"), ConflictType.TypeOfConflict.Track, 8, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(-31)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(3, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "Non Control Switch at S54"), ConflictType.TypeOfConflict.Track, 9, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(-33)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(2, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "False Occupancy at S12"), ConflictType.TypeOfConflict.Switch, 10, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(-35)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(1, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "Switch Route Blocked Away From Route at S85"), ConflictType.TypeOfConflict.Switch, 11, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.Manual), DateTime.Now.AddMinutes(-37)));
        //    MyConflictsPast.Add(Conflict.CreateInstance(0, ConflictEntity.CreateInstance(ConflictEntity.EntityType.Switch, "Switch Route Blocked Away From Route at S39"), ConflictType.TypeOfConflict.Switch, 12, ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.RouteAround), DateTime.Now.AddMinutes(-39)));

        //    TestSerialize();
        //}

        //public static void TestSerialize()
        //{
        //    var temp = Conflict.CreateInstance(11,
        //        ConflictEntity.CreateInstance(ConflictEntity.EntityType.Train, "9876"),
        //        ConflictType.TypeOfConflict.Train, 0,
        //        ConflictResolution.CreateInstance(ConflictResolution.TypeOfResolution.HoldTrain),
        //        DateTime.Now.AddMinutes(15));
        //    var obj = JsonConvert.SerializeObject(temp);
        //    var dobj = JsonConvert.DeserializeObject<Conflict>(obj);
        //    var serializer = new JsonSerializer();
        //    const string filePath = @"c:\temp\json.txt";

        //    using (var sw = new StreamWriter(filePath)) 
        //    using (JsonWriter writer = new JsonTextWriter(sw))
        //    {
        //        serializer.Serialize(writer, temp);
        //    }

        //    //using (var sw = new StreamReader(filePath))
        //    //using (var reader = new JsonTextReader(sw))
        //    //{
        //    //    return serializer.Deserialize(reader);
        //    //}
        //}
    }

 
}
