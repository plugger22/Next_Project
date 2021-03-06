﻿using System;
using System.Collections.Generic;

namespace Next_Game.Cartographic
{

    public enum Cluster {Sea, Mountain, Forest} //NOTE: don't change order as ties in with data from map.InitialiseTerrain
    public enum GeoType {Large_Mtn, Medium_Mtn, Small_Mtn, Large_Forest, Medium_Forest, Small_Forest, Large_Sea, Medium_Sea, Small_Sea, Count}

    /// <summary>
    /// Clusters of geo objects such as sea, mountain or forest zones (orthagonol clusters)
    /// </summary>
    class GeoCluster : IEquatable<GeoCluster>
    {
        public string Name { get; set; } = "Unknown";
        public string Description { get; set; } = "No description provided";
        public int Size { get; }
        public int GeoID { get; }
        public int Archetype { get; set; } //arcID, if any
        public Cluster Terrain { get; }
        public GeoType Type { get; }
        private List<int> listOfSecrets;
        private List<int> listOfFollowerEvents; //Archetype events
        private List<int> listOfPlayerEvents; //Archetype events
        private List<int> listOfPorts; //Sea clusters only (locID of orthoganl adjoining cities)

        public GeoCluster()
        {
            listOfSecrets = new List<int>();
            listOfFollowerEvents = new List<int>();
            listOfPlayerEvents = new List<int>();
            listOfPorts = new List<int>();
        }

        //default constructor
        public GeoCluster(int geoID, int type, int size)
        {
            listOfSecrets = new List<int>();
            listOfFollowerEvents = new List<int>();
            listOfPlayerEvents = new List<int>();
            listOfPorts = new List<int>();
            this.GeoID = geoID;
            Terrain = (Cluster)type;
            this.Size = size;
            //determine type
            int small = Game.constant.GetValue(Global.TERRAIN_SMALL);
            int seaLarge = Game.constant.GetValue(Global.SEA_LARGE);
            int mountainLarge = Game.constant.GetValue(Global.MOUNTAIN_LARGE);
            int forestLarge = Game.constant.GetValue(Global.FOREST_LARGE);
            switch (Terrain)
            {
                case Cluster.Sea:
                    if (size <= small) { Type = GeoType.Small_Sea; }
                    else if (size >= seaLarge) { Type = GeoType.Large_Sea; }
                    else { Type = GeoType.Medium_Sea; }
                    break;
                case Cluster.Mountain:
                    if (size <= small) { Type = GeoType.Small_Mtn; }
                    else if (size >= mountainLarge) { Type = GeoType.Large_Mtn; }
                    else { Type = GeoType.Medium_Mtn; }
                    break;
                case Cluster.Forest:
                    if (size <= small) { Type = GeoType.Small_Forest; }
                    else if (size >= forestLarge) { Type = GeoType.Large_Forest; }
                    else { Type = GeoType.Medium_Forest; }
                    break;
            }
        }

        /// <summary>
        /// IEquatable
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            GeoCluster cluster = obj as GeoCluster;
            if (cluster == null) return false;
            else return Equals(cluster);
        }

        /// <summary>
        /// IEquatable
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        { return GeoID; }

        /// <summary>
        /// IEquatable (match on GeoID)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(GeoCluster other)
        {
            if (other == null) return false;
            return (this.GeoID.Equals(other.GeoID));
        }

        /// <summary>
        /// add Follower events to the Geocluster
        /// </summary>
        /// <param name="listArchEvents"></param>
        public void SetFollowerEvents(List<int> listArchEvents)
        {
            if (listArchEvents != null)
            { listOfFollowerEvents.AddRange(listArchEvents); }
            else
            { Game.logStart?.Write("Invalid list of Follower Events input (null) -> No follower events for this archetype"); }
        }


        public List<int> GetFollowerEvents()
        { return listOfFollowerEvents; }

        public int GetNumFollowerEvents()
        { return listOfFollowerEvents.Count; }

        /// <summary>
        /// add Player events to the Geocluster
        /// </summary>
        /// <param name="listArchEvents"></param>
        public void SetPlayerEvents(List<int> listArchEvents)
        {
            if (listArchEvents != null)
            { listOfPlayerEvents.AddRange(listArchEvents); }
            else
            { Game.logStart?.Write("Invalid list of Player Events input (null) -> No player events for this archetype"); }
        }


        public List<int> GetPlayerEvents()
        { return listOfPlayerEvents; }

        public int GetNumPlayerEvents()
        { return listOfPlayerEvents.Count; }


        /// <summary>
        /// Add a port to a SeaCluster (only if Port Not already on list)
        /// </summary>
        /// <param name="locID"></param>
        public void AddPort(int locID)
        {
            if (locID > 0)
            {
                if (listOfPorts.Contains(locID) == false)
                {
                    listOfPorts.Add(locID);
                    Game.logStart?.Write(string.Format("LocID {0} has been added to the ListPorts for GeoID {1}", locID, GeoID));
                }
                else { Game.logStart?.Write(string.Format("[Notification] LocID {0} is already present in ListPorts for GeoID {1}", locID, GeoID)); }
            }
            else
            { Game.SetError(new Error(214, "Invalid LocID (zero or less)")); }
        }

        /// <summary>
        /// returns number of ports in cluster (sea only but will return zero if called for a non-sea cluster)
        /// </summary>
        /// <returns></returns>
        public int GetNumPorts()
        { return listOfPorts.Count; }

        public List<int> GetPorts()
        { return listOfPorts; }

        //methods above here
    }
}
