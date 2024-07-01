using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using ConflictManagementLibrary.Model.Reservation;
using ConflictManagementLibrary.Model.Trip;

namespace ConflictManagementLibrary.Network
{
    public class Station
    {
        [JsonProperty(Order = 1)] public int MyReferenceNumber { get; set; }
        [JsonProperty(Order = 2)] public string StationName { get; set; }
        [JsonProperty(Order = 4)] public List<Node> MyNodes = new List<Node>();
        [JsonProperty(Order = 3)] public string Abbreviation = "";
        [JsonIgnore] public bool HasStationNecks { get; set; } = false;
        private List<TimedLocation> myTimeSlots { get; set; } = new List<TimedLocation>();
        public List<Reservation> MyReservations { get; set; } = new List<Reservation>();
        public Station()
        {

        }
        public string GetStationInformation()
        {
            var station = this;
            var s = new StringBuilder();
            s.AppendLine("\nName<" + station.StationName + "> Abbreviation<" + station.Abbreviation + "> Ref#<" +
                         station.MyReferenceNumber + ">");
            foreach (var node in station.MyNodes)
            {
                s.AppendLine("\t Node<" + node.MyReferenceNumber + ">");
                s.AppendLine("\t My Left Links");
                foreach (var ll in node.MyLeftLinks)
                {
                    s.AppendLine("\t\t Link Ref<" + ll.MyReferenceNumber + "> Description<" + ll.MyDescription + ">");
                }
                s.AppendLine("\t My Right Links");
                foreach (var rl in node.MyRightLinks)
                {
                    s.AppendLine("\t\t Link Ref<" + rl.MyReferenceNumber + "> Description<" + rl.MyDescription + ">");
                }
                s.AppendLine("\t My Paths");
                foreach (var path in node.MyPaths)
                {
                    s.AppendLine("\t\t Path Ref<" + path.MyReferenceNumber + "> Route Name<" + path.MyRouteName + ">");
                }

            }
            return s.ToString();
        }
        public bool CheckStationReserved(TimedLocation timedLocation)
        {
            lock (myTimeSlots)
            {
                var tmSlotFwd = myTimeSlots.Any(r => (r.ArrivalTimeActual >= timedLocation.ArrivalTimeActual && r.DepartureTimeActual <= timedLocation.DepartureTimeActual));
                var tmSlotRev = myTimeSlots.Any(r => (r.ArrivalTimeActual >= timedLocation.DepartureTimeActual && r.DepartureTimeActual <= timedLocation.ArrivalTimeActual));
                return (tmSlotRev | tmSlotFwd);
            }
        }
        public bool AddReservations(Reservation reservation)
        {
            try
            {
                lock (MyReservations)
                {
                    //if (CheckStationReserved(timedLocation))
                    //{
                    //    return false;
                    //}
                    MyReservations.Add(reservation);
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public bool RemoveReservations(Reservation reservation)
        {
            try
            {
                lock (MyReservations)
                {
                    //var tmSlotFwd = myTimeSlots.FirstOrDefault(r => (r.ArrivalTime >= timedLocation.ArrivalTime && r.DepartureTime <= timedLocation.DepartureTime));
                    //var tmSlotRev = myTimeSlots.FirstOrDefault(r => (r.ArrivalTime >= timedLocation.DepartureTime && r.DepartureTime <= timedLocation.ArrivalTime));
                    //if (tmSlotFwd != null)
                    //{
                    //    myTimeSlots.Remove(tmSlotFwd);
                    //}
                    //if (tmSlotRev != null)
                    //{
                    //    myTimeSlots.Remove(tmSlotRev);
                    //}
                    MyReservations.Remove(reservation);
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
        public Reservation CheckStationNeckReservationConflict(Reservation reservationToCheck, Path pathToRight, Path pathToLeft, Trip tripToCheck)
        {
            try
            {
                lock (MyReservations)
                {
                    foreach (var r in MyReservations)
                    {
                        if (reservationToCheck.MyLink == null || r.MyLink == null) continue;
                        if (reservationToCheck.MyTripCode == r.MyTripCode) continue;
                        if (r.MyLink.MyReferenceNumber == pathToRight.MyConnectionLeft) 
                            //if (r.MyLink.MyReferenceNumber == pathToLeft.MyConnectionRight)
                        {
                            var endTime = reservationToCheck.TimeEnd;
                            if (endTime <= r.TimeEnd && endTime >= r.TimeBegin)
                            {
                                return r;
                            }
                            if (endTime >= r.TimeEnd && endTime <= r.TimeBegin)
                            {
                                return r;
                            }

                        }
                    }
                    //foreach (var r in MyReservations.Where(r => !Equals(r, reservationToCheck)))
                    //{
                    //    if (reservationToCheck.MyLink == null || r.MyLink == null) continue;
                    //    if (reservationToCheck.MyTripCode == r.MyTripCode) continue;
                    //    if (reservationToCheck.MyLink.MyReferenceNumber != r.MyLink.MyReferenceNumber) continue;
                    //    if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                    //    {
                    //        return r;
                    //    }
                    //    if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                    //    {
                    //        return r;
                    //    }

                    //    if (reservationToCheck.MyPath == null || r.MyPath == null) continue;
                    //    if (reservationToCheck.MyPath.MyReferenceNumber != r.MyPath.MyReferenceNumber) continue;
                    //    if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                    //    {
                    //        return r;
                    //    }
                    //    if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                    //    {
                    //        return r;
                    //    }
                    //}
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return null;
        }

        public Reservation CheckReservationConflict(Reservation reservationToCheck)
        {
            try
            {
                lock (MyReservations)
                {
                    foreach (var r in MyReservations.Where(r => !Equals(r, reservationToCheck)))
                    {
                        if (reservationToCheck.MyLink == null || r.MyLink == null) continue;
                        if (reservationToCheck.MyTripCode == r.MyTripCode) continue;
                        if (reservationToCheck.MyLink.MyReferenceNumber != r.MyLink.MyReferenceNumber) continue;
                        if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                        {
                            return r;
                        }
                        if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                        {
                            return r;
                        }

                        if (reservationToCheck.MyPath == null || r.MyPath == null) continue;
                        if (reservationToCheck.MyPath.MyReferenceNumber != r.MyPath.MyReferenceNumber) continue;
                        if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                        {
                            return r;
                        }
                        if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                        {
                            return r;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return null;
        }
        public List<Reservation> CheckReservationConflict(Reservation reservationToCheck, bool useNewFunction = true)
        {
            List<Reservation> theReservations = new List<Reservation>();

            try
            {
                lock (MyReservations)
                {
                    foreach (var r in MyReservations.Where(r => !Equals(r, reservationToCheck)))
                    {
                        if (reservationToCheck.MyLink == null || r.MyLink == null) continue;
                        if (reservationToCheck.MyTripCode == r.MyTripCode) continue;
                        if (reservationToCheck.MyLink.MyReferenceNumber != r.MyLink.MyReferenceNumber) continue;
                        if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                        {
                            theReservations.Add(r);
                        }
                        if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                        {
                            theReservations.Add(r);
                        }

                        if (reservationToCheck.MyPath == null || r.MyPath == null) continue;
                        if (reservationToCheck.MyPath.MyReferenceNumber != r.MyPath.MyReferenceNumber) continue;
                        if (reservationToCheck.TimeBegin >= r.TimeBegin && reservationToCheck.TimeBegin <= r.TimeEnd)
                        {
                            theReservations.Add(r);
                        }
                        if (reservationToCheck.TimeEnd >= r.TimeBegin && reservationToCheck.TimeEnd <= r.TimeEnd)
                        {
                            theReservations.Add(r);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return theReservations;
        }

    }
}