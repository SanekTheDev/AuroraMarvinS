namespace AuroraMarvin
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Serialization;
    using AuroraMarvin.Aurora;
    using Microsoft.Msagl.Drawing;

    public class AuroraMarvinController : IAuroraMarvinController
    {
        private readonly IAuroraMarvinView view;
        private readonly IAuroraMarvinModel model;

        public AuroraMarvinController(IAuroraMarvinView v, IAuroraMarvinModel m)
        {
            this.view = v;
            this.model = m;
            this.model.OpenMarvinDatabase();
            this.view.SetController(this);
        }

        public void GetGames()
        {
            DataTable games = this.model.GetGames();
            this.view.SetGames(games);
        }

        public void GetRaces()
        {
            DataTable races = this.model.GetRaces();
            this.view.SetRaces(races);
        }

        public bool CheckForChangedDatabase()
        {
            return this.model.CheckForChangedDatabase();
        }

        public void SavePopulationResources()
        {
            DataTable r = this.model.GetPopulationResources();
            this.model.SavePopulationResources(r);
        }

        public void SaveWealthPoints()
        {
            DataTable r = this.model.GetWealthPoints();
            this.model.SaveWealthPoints(r);
        }

        public DataTable GetPopulationResources(int popId)
        {
            return this.model.GetSavedPopulationResources(popId);
        }

        public void GetDesignsForHull(int hull)
        {
            DataTable shipDesigns = this.model.GetShipDesignsForHull(hull);
            this.view.SetDesignsForHull(shipDesigns);
        }

        public void GetShipDesign(string name)
        {
            DataTable design = this.model.GetShipDesign(name);
            this.view.SetShipDesign(design);
        }

        public void SaveShipDesign(string hull, string name, string design)
        {
            this.model.SaveShipDesign(hull, name, design);
        }

        public void SetDatabaseFile(string dbfile)
        {
            this.model.Dbfile = dbfile;
            this.model.OpenAuroraDatabase();
        }

        public void SetGame(int game)
        {
            this.model.Game = game;
        }

        public void SetRace(int race)
        {
            this.model.Race = race;
        }

        public void UpdateNotes(string text)
        {
            this.model.UpdateNotes(false, text);
        }

        public void UpdateGameNotes(string text)
        {
            this.model.UpdateNotes(true, text);
        }

        public Graph GetTechTree()
        {
            DataTable tech = this.model.GetTechData();
            Graph techtree = new Graph("techtree");

            // The UI uses a deterministic custom layout for the Tech Tree. The graph
            // remains the source of truth for technologies and prerequisites.
            var researchRows = new List<DataRow>();
            var researchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in tech.Rows)
            {
                bool automaticResearch = false;
                if (tech.Columns.Contains("AutomaticResearch") && row["AutomaticResearch"] != DBNull.Value)
                {
                    string automaticValue = row["AutomaticResearch"].ToString();
                    automaticResearch = automaticValue == "1" || automaticValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                bool startingSystem = false;
                if (tech.Columns.Contains("StartingSystem") && row["StartingSystem"] != DBNull.Value)
                {
                    string startingValue = row["StartingSystem"].ToString();
                    startingSystem = startingValue == "1" || startingValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                string name = row["Name"].ToString().Trim();

                // BioEnergy and Swarm technologies are game-system/NPR technologies
                // and are not part of the player's normal research tree. Keep them
                // out of the visual Tech Tree while leaving the underlying Aurora
                // database data completely untouched.
                bool excludedSystemTechnology =
                    name.IndexOf("BioEnergy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Swarm", StringComparison.OrdinalIgnoreCase) >= 0;

                if (automaticResearch ||
                    startingSystem ||
                    excludedSystemTechnology ||
                    string.Equals(name, "Component Creation", StringComparison.OrdinalIgnoreCase))
                    continue;

                researchRows.Add(row);
                researchIds.Add(row["TechSystemID"].ToString());
            }

            foreach (DataRow row in researchRows)
            {
                string id = row["TechSystemID"].ToString();
                string name = row["Name"].ToString();
                string fieldName = row["FieldName"].ToString();
                long developCost = 0;
                if (tech.Columns.Contains("DevelopCost") && row["DevelopCost"] != DBNull.Value)
                    long.TryParse(row["DevelopCost"].ToString(), out developCost);

                bool researched = false;
                if (tech.Columns.Contains("Researched") && row["Researched"] != DBNull.Value)
                    researched = row["Researched"].ToString() == "1" || row["Researched"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

                bool currentlyResearching = false;
                if (tech.Columns.Contains("CurrentlyResearching") && row["CurrentlyResearching"] != DBNull.Value)
                    currentlyResearching = row["CurrentlyResearching"].ToString() == "1" || row["CurrentlyResearching"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

                Node node = techtree.AddNode(id);
                node.LabelText = name;
                node.UserData = new TechTreeNodeInfo
                {
                    FieldName = string.IsNullOrWhiteSpace(fieldName) ? "Other" : fieldName,
                    Name = name,
                    DevelopCost = developCost,
                    Researched = researched,
                    CurrentlyResearching = currentlyResearching
                };
            }

            foreach (DataRow row in researchRows)
            {
                string id = row["TechSystemID"].ToString();
                string prereq1 = row["Prerequisite1"].ToString();
                string prereq2 = row["Prerequisite2"].ToString();
                if (prereq1 != "0" && researchIds.Contains(prereq1))
                    techtree.AddEdge(prereq1, id);
                if (prereq2 != "0" && researchIds.Contains(prereq2))
                    techtree.AddEdge(prereq2, id);
            }

            return techtree;
        }

        public void GetData()
        {
            this.GetGameTime();
            this.GetPopulations();
            this.GetShips();
            this.GetHullDescriptions();
            this.GetNotes();
            this.GetResources();
            this.GetFuelInTankers();
            this.GetEventColours();
            this.view.ClearOverviewMessages();
            this.view.AddOverviewMessage("ShowAetherRiftGrowthRateReductionToolStripMenuItem", this.GetAetherRiftGrowthRateReduction());
            this.view.AddOverviewMessage("ShowDamagedShipsToolStripMenuItem", this.GetDamagedShips());
            this.view.AddOverviewMessage("ShowLowCrewMoraleToolStripMenuItem", this.GetLowCrewMorale());
            this.view.AddOverviewMessage("ShowFreeConstructionFactoriesToolStripMenuItem", this.GetFreeConstructionFactories());
            this.view.AddOverviewMessage("ShowFreeOrdnanceFactoriesToolStripMenuItem", this.GetFreeOrdnanceFactories());
            this.view.AddOverviewMessage("ShowFreeFighterFactoriesToolStripMenuItem", this.GetFreeFighterFactories());
            this.view.AddOverviewMessage("ShowObsoleteShipsToolStripMenuItem", this.GetObsoleteShips());
            this.view.AddOverviewMessage("ShowUnusedTerraformersToolStripMenuItem", this.GetUnusedTerraformers());
            this.view.AddOverviewMessage("ShowUnusedMinesToolStripMenuItem", this.GetUnusedMines());
            this.view.AddOverviewMessage("ShowShipsWithArmorDamageToolStripMenuItem", this.GetShipsWithArmorDamage());
            this.view.AddOverviewMessage("ShowWrecksToolStripMenuItem", this.GetWrecks());
            this.view.AddOverviewMessage("ShowObsoleteTooledShipyardsToolStripMenuItem", this.GetObsoleteTooledShipyards());
            this.view.AddOverviewMessage("ShowMissingSectorCommandersToolStripMenuItem", this.GetMissingSectorCommanders());
            this.view.AddOverviewMessage("ShowShipWithoutMspToolStripMenuItem", this.GetShipWithoutMsp());
            this.view.AddOverviewMessage("ShowShipWithLowMspToolStripMenuItem", this.GetShipWithLowMsp());
            this.view.AddOverviewMessage("ShowMissingAdminCommandersToolStripMenuItem", this.GetMissingAdminCommanders());
            this.view.AddOverviewMessage("ShowCivMinesWithoutMassDriverDestinationToolStripMenuItem", this.GetCivMinesWithoutMassDriverDestination());
            this.view.AddOverviewMessage("ShowTaxedCivMinesToolStripMenuItem", this.GetTaxedCivMines());
            this.view.AddOverviewMessage("ShowLowPopulationEfficienyToolStripMenuItem", this.GetLowPopulationEfficieny());
            this.view.AddOverviewMessage("ShowSelfSustainingColonistDestinationToolStripMenuItem", this.GetSelfSustainingColonistDestination());
            this.view.AddOverviewMessage("ShowFullTrainedShipsinTrainingFleetsToolStripMenuItem", this.GetFullTrainedShipsinTrainingFleets());
            this.view.AddOverviewMessage("ShowLifePodsToolStripMenuItem", this.GetLifePods());
            this.view.AddOverviewMessage("ShowPopulationsWithoutGovernorToolStripMenuItem", this.GetPopulationsWithoutGovernor());
            this.view.AddOverviewMessage("ShowOpenFireFCToolStripMenuItem", this.GetOpenFireFC());
            this.view.AddOverviewMessage("ShowResearchFieldMismatchToolStripMenuItem", this.GetResearchFieldMismatch());
            this.view.AddOverviewMessage("ShowResearchWithoutResearchFacilityToolStripMenuItem", this.GetResearchWithoutResearchFacility());
            this.view.AddOverviewMessage("ShowSystemsWithUnsurveyedBodiesToolStripMenuItem", this.GetSystemsWithUnsurveyedBodies());
            this.view.AddOverviewMessage("ShowIdleSoriumHarvestersToolStripMenuItem", this.GetIdleSoriumHarvesters());
            this.view.AddOverviewMessage("ShowIdleOrbitalMinersToolStripMenuItem", this.GetIdleOrbitalMiners());
            this.view.AddOverviewMessage("ShowPopulationsWithGroundSurveyPotentialToolStripMenuItem", this.GetPopulationsWithGroundSurveyPotential());
            this.view.AddOverviewMessage("ShowIdleGeosurveyFormationsToolStripMenuItem", this.GetIdleGeosurveyFormations());
            this.view.AddOverviewMessage("ShowDormantAncientConstructsToolStripMenuItem", this.GetDormantAncientConstructs());
            this.view.AddOverviewMessage("ShowNotActiveAncientConstructsToolStripMenuItem", this.GetNotActiveAncientConstructs());
            this.view.AddOverviewMessage("ShowHostileShipContactsToolStripMenuItem", this.GetHostileShipContacts());
            this.view.AddOverviewMessage("ShowHostileGroundForceContactsToolStripMenuItem", this.GetHostileGroundForceContacts());
            this.view.AddOverviewMessage("ShowIdleShipyardsToolStripMenuItem", this.GetIdleShipyards());
            this.view.AddOverviewMessage("ShowIdleGroundForceConstructionComplexToolStripMenuItem", this.GetIdleGroundForceConstructionComplex());
            this.view.RefreshOverviewMessage();
        }

        public void SetMenuItemState(string name, bool @checked)
        {
            this.model.SetMenuItemState(name, @checked);
        }

        public DataTable GetMenuItemState()
        {
            return this.model.GetMenuItemState();
        }

        private static string ToRoman(int number)
        {
            if (number < 1 || number > 3999)
            {
                return string.Empty;
            }

            if (number >= 1000)
            {
                return "M" + ToRoman(number - 1000);
            }

            if (number >= 900)
            {
                return "CM" + ToRoman(number - 900);
            }

            if (number >= 500)
            {
                return "D" + ToRoman(number - 500);
            }

            if (number >= 400)
            {
                return "CD" + ToRoman(number - 400);
            }

            if (number >= 100)
            {
                return "C" + ToRoman(number - 100);
            }

            if (number >= 90)
            {
                return "XC" + ToRoman(number - 90);
            }

            if (number >= 50)
            {
                return "L" + ToRoman(number - 50);
            }

            if (number >= 40)
            {
                return "XL" + ToRoman(number - 40);
            }

            if (number >= 10)
            {
                return "X" + ToRoman(number - 10);
            }

            if (number >= 9)
            {
                return "IX" + ToRoman(number - 9);
            }

            if (number >= 5)
            {
                return "V" + ToRoman(number - 5);
            }

            if (number >= 4)
            {
                return "IV" + ToRoman(number - 4);
            }

            if (number >= 1)
            {
                return "I" + ToRoman(number - 1);
            }

            return string.Empty;
        }

        private void GetFuelInTankers()
        {
            DataTable fuel = this.model.GetFuelInTankers();
            this.view.SetFuelInTankers(fuel);
        }

        private List<string> GetAetherRiftGrowthRateReduction()
        {
            DataTable riftReduction = this.model.GetAetherRiftGrowthRateReduction();
            List<string> msg = new List<string>
            {
                "Known aether rift growth rate reduction: " + riftReduction.Rows[0]["RiftReduction"].ToString() + "%",
            };
            return msg;
        }

        private List<string> GetHostileGroundForceContacts()
        {
            DataTable contacts = this.model.GetHostileGroundForceContacts();
            List<string> msg = new List<string>();
            foreach (DataRow row in contacts.Rows)
            {
                DataTable timestamp = this.model.GetGameTime();
                DateTime dtDateTime = new DateTime(int.Parse(timestamp.Rows[0]["StartYear"].ToString()), 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
                DateTime lastContactTime = dtDateTime.AddSeconds(double.Parse(row["LastUpdate"].ToString())).ToLocalTime();

                msg.Add("Hostile ground force contact [" + row["Abbrev"].ToString() + "] " + row["Contact"].ToString() + " of " + row["AlienRaceName"].ToString() + " detected in " + row["Name"].ToString() + ". Last contact: " + lastContactTime + ".");
            }

            return msg;
        }

        private List<string> GetIdleShipyards()
        {
            DataTable shipyards = this.model.GetIdleShipyards();
            List<string> msg = new List<string>();
            foreach (DataRow row in shipyards.Rows)
            {
                msg.Add("Idle shipyard " + row["ShipyardName"].ToString() + " on " + row["PopName"].ToString());
            }

            return msg;
        }

        private List<string> GetIdleGroundForceConstructionComplex()
        {
            DataTable gfcc = this.model.GetIdleGroundForceConstructionComplex();
            List<string> msg = new List<string>();
            foreach (DataRow row in gfcc.Rows)
            {
                msg.Add("Idle ground force construction complex on " + row["PopName"].ToString() + ". Facilities available: " + row["Amount"].ToString() + ". Queued tasks: " + row["QueueLength"].ToString());
            }

            return msg;
        }

        private List<string> GetHostileShipContacts()
        {
            DataTable contacts = this.model.GetHostileShipContacts();
            List<string> msg = new List<string>();
            foreach (DataRow row in contacts.Rows)
            {
                DataTable timestamp = this.model.GetGameTime();
                DateTime dtDateTime = new DateTime(int.Parse(timestamp.Rows[0]["StartYear"].ToString()), 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
                DateTime lastContactTime = dtDateTime.AddSeconds(double.Parse(row["LastUpdate"].ToString())).ToLocalTime();

                msg.Add("Hostile ship contact [" + row["Abbrev"].ToString() + "] " + row["Contact"].ToString() + " of " + row["AlienRaceName"].ToString() + " detected in " + row["Name"].ToString() + ". Last contact: " + lastContactTime + ".");
            }

            return msg;
        }

        private List<string> GetDormantAncientConstructs()
        {
            DataTable systems = this.model.GetDormantAncientConstructs();
            List<string> msg = new List<string>();
            foreach (DataRow row in systems.Rows)
            {
                msg.Add("Dormant ancient construct located in system " + row["Name"].ToString());
            }

            return msg;
        }

        private List<string> GetNotActiveAncientConstructs()
        {
            DataTable populations = this.model.GetNotActiveAncientConstructs();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                msg.Add("Ancient construct on " + row["PopName"].ToString() + " is not active. Current population: " + row["Population"] + "m. Research field: " + row["FieldName"].ToString() + ". Bonus: " + row["Bonus"].ToString());
            }

            return msg;
        }

        private List<string> GetPopulationsWithGroundSurveyPotential()
        {
            DataTable population = this.model.GetPopulationsWithGroundSurveyPotential();
            List<string> msg = new List<string>();
            foreach (DataRow row in population.Rows)
            {
                msg.Add("Population " + row["PopName"].ToString() + " in system " + row["Name"].ToString() + " has a ground survey potential of " + (Aurora.GroundMineralSurvey)int.Parse(row["GroundMineralSurvey"].ToString()));
            }

            return msg;
        }

        private List<string> GetIdleGeosurveyFormations()
        {
            DataTable formations = this.model.GetIdleGeosurveyFormations();
            List<string> msg = new List<string>();
            foreach (DataRow row in formations.Rows)
            {
                msg.Add("Geosurvey formation [" + row["Abbreviation"].ToString() + "] " + row["Formation"].ToString() + " is idle on " + row["PopName"].ToString() + " in system " + row["Name"].ToString());
            }

            return msg;
        }

        private List<string> GetIdleOrbitalMiners()
        {
            DataTable diameter = this.model.GetOrbitalMiningDiameter();
            List<string> msg = new List<string>();

            // A game may not have a Maximum Orbital Mining Diameter tech yet,
            // or the selected game may have just been deleted. In that case
            // the MAX() query returns a NULL value. Do not let that propagate
            // into double.Parse(), which would crash the application.
            if (diameter.Rows.Count == 0 || diameter.Rows[0]["MaxDiameter"] == DBNull.Value)
            {
                return msg;
            }

            double maxdiameter;
            if (!double.TryParse(diameter.Rows[0]["MaxDiameter"].ToString(), out maxdiameter))
            {
                return msg;
            }

            DataTable ships = this.model.GetIdleOrbitalMiners();
            foreach (DataRow row in ships.Rows)
            {
                int s;
                int p;
                double d;
                double minerals;

                if (!int.TryParse(row["Component"].ToString(), out s)
                    || !int.TryParse(row["PlanetNumber"].ToString(), out p)
                    || !double.TryParse(row["Radius"].ToString(), out d)
                    || !double.TryParse(row["Minerals"].ToString(), out minerals))
                {
                    continue;
                }

                char sol = Convert.ToChar(s + 64);
                d *= 2;

                bool show = false;
                string m = string.Empty;

                if (d > maxdiameter)
                {
                    show = true;
                    m = "Body diameter " + d.ToString() + "km is larger then maximum orbital mining diameter: " + maxdiameter.ToString() + "km";
                }

                if (minerals == 0.0)
                {
                    show = true;
                    m = "No minerals on body left";
                }

                if (show)
                {
                    string ms = "Orbital miner " + row["ShipName"].ToString() + " of fleet " + row["FleetName"] + " orbiting " + row["Name"].ToString() + "-" + sol + " " + ToRoman(p) + " " + row["BodyName"] + " is idle: " + m;
                    msg.Add(Regex.Replace(ms, @"\s+", " "));
                }
            }

            return msg;
        }

        private List<string> GetIdleSoriumHarvesters()
        {
            DataTable ships = this.model.GetIdleSoriumHarvesters();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                int s = int.Parse(row["Component"].ToString()) + 64;
                char sol = Convert.ToChar(s);
                int p = int.Parse(row["PlanetNumber"].ToString());

                string ms = "Sorium harvester " + row["ShipName"].ToString() + " of fleet " + row["FleetName"] + " orbiting " + row["Name"].ToString() + "-" + sol + " " + ToRoman(p) + " " + row["BodyName"] + " is idle";
                msg.Add(Regex.Replace(ms, @"\s+", " "));
            }

            return msg;
        }

        private List<string> GetSystemsWithUnsurveyedBodies()
        {
            DataTable systems = this.model.GetSystemsWithUnsurveyedBodies();
            List<string> msg = new List<string>();
            foreach (DataRow row in systems.Rows)
            {
                msg.Add("System " + row["Name"].ToString() + " is not fully geo surveyed. Bodies: " + row["SystemBodies"].ToString() + ". Surveyed bodies: " + row["SurveyedBodies"].ToString());
            }

            return msg;
        }

        private List<string> GetTaxedCivMines()
        {
            DataTable populations = this.model.GetTaxedCivMines();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                msg.Add("Minerals from " + row["PopName"].ToString() + " in system " + row["Name"].ToString() + " are taxed and not purchased");
            }

            return msg;
        }

        private List<string> GetCivMinesWithoutMassDriverDestination()
        {
            DataTable populations = this.model.GetCivMinesWithoutMassDriverDestination();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                msg.Add("Purchase minerals from " + row["PopName"].ToString() + " but no mass driver destination is set");
            }

            return msg;
        }

        private List<string> GetDamagedShips()
        {
            DataTable damagedShips = this.model.GetDamagedShips();
            List<string> msg = new List<string>();
            foreach (DataRow row in damagedShips.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " has damaged component: " + row["Name"].ToString());
            }

            return msg;
        }

        private List<string> GetFreeOrdnanceFactories()
        {
            DataTable populations = this.model.GetFreeOrdnanceFactories();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                if (Convert.ToDouble(row["Percentage"].ToString()) < 100)
                {
                    msg.Add("Planet " + row["PopName"].ToString() + " has unused ordnance factories. Total number of factories: " + row["Amount"].ToString() + ". Used percentage: " + row["Percentage"].ToString() + "%");
                }
            }

            return msg;
        }

        private List<string> GetFreeFighterFactories()
        {
            DataTable populations = this.model.GetFreeFighterFactories();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                if (Convert.ToDouble(row["Percentage"].ToString()) < 100)
                {
                    msg.Add("Planet " + row["PopName"].ToString() + " has unused fighter factories. Total number of factories: " + row["Amount"].ToString() + ". Used percentage: " + row["Percentage"].ToString() + "%");
                }
            }

            return msg;
        }

        private List<string> GetOpenFireFC()
        {
            DataTable fcs = this.model.GetOpenFireFC();
            List<string> msg = new List<string>();
            foreach (DataRow row in fcs.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " of fleet " + row["FleetName"].ToString() + " has FC #" + row["FCNum"].ToString() + " with open fire set");
            }

            return msg;
        }

        private List<string> GetUnusedMines()
        {
            DataTable mines = this.model.GetUnusedMines();
            List<string> msg = new List<string>();
            foreach (DataRow row in mines.Rows)
            {
                string amount = row["Amount"].ToString();
                string plural = string.Empty;
                if (amount != "1")
                {
                    plural = "s";
                }

                msg.Add(row["Amount"].ToString() + " " + row["Name"].ToString() + plural + " on " + row["PopName"].ToString() + " but body has no minerals left");
            }

            return msg;
        }

        private List<string> GetFreeConstructionFactories()
        {
            DataTable populations = this.model.GetFreeConstructionFactories();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                if (Convert.ToDouble(row["Percentage"].ToString()) < 100)
                {
                    msg.Add("Planet " + row["PopName"].ToString() + " has unused construction factories. Total number of factories: " + row["Amount"].ToString() + ". Used percentage: " + row["Percentage"].ToString() + "%");
                }
            }

            return msg;
        }

        private List<string> GetFullTrainedShipsinTrainingFleets()
        {
            DataTable ships = this.model.GetFullTrainedShipsinTrainingFleets();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " is assigned to training fleet " + row["FleetName"].ToString() + " but is fully trained");
            }

            return msg;
        }

        private void GetGameTime()
        {
            DataTable timestamp = this.model.GetGameTime();
            int startyear = int.Parse(timestamp.Rows[0]["StartYear"].ToString());
            DateTime dtDateTime = new DateTime(startyear, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            DateTime gameTime = dtDateTime.AddSeconds(double.Parse(timestamp.Rows[0]["GameTime"].ToString())).ToLocalTime();
            this.view.SetGameTime(gameTime);
            this.view.SetGameStartTime(startyear);
        }

        private void GetHullDescriptions()
        {
            DataTable hullDescriptions = this.model.GetHullDescriptions();
            this.view.SetHullDescriptions(hullDescriptions);
        }

        private List<string> GetLifePods()
        {
            DataTable lifepods = this.model.GetLifePods();
            List<string> msg = new List<string>();
            foreach (DataRow row in lifepods.Rows)
            {
                msg.Add("Lifepod from ship " + row["ShipName"].ToString() + " with " + row["Crew"].ToString() + " survivors found in system " + row["Name"]);
            }

            return msg;
        }

        private List<string> GetLowCrewMorale()
        {
            DataTable ships = this.model.GetLowCrewMorale();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                string p = Convert.ToInt32(float.Parse(row["CrewMorale"].ToString()) * 100).ToString();
                msg.Add("Ship " + row["ShipName"].ToString() + " has low crew morale: " + p + "%");
            }

            return msg;
        }

        private List<string> GetLowPopulationEfficieny()
        {
            DataTable populations = this.model.GetLowPopulationEfficieny();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                string e = Convert.ToInt32(float.Parse(row["Efficiency"].ToString()) * 100).ToString();
                msg.Add("Population " + row["PopName"].ToString() + " has low manufacturing efficiency modifier: " + e + "%");
            }

            return msg;
        }

        private List<string> GetMissingAdminCommanders()
        {
            DataTable commands = this.model.GetMissingAdminCommanders();
            List<string> msg = new List<string>();
            foreach (DataRow row in commands.Rows)
            {
                msg.Add("Naval Admin Command " + row["AdminCommandName"].ToString() + " has no commander assigned");
            }

            return msg;
        }

        private List<string> GetMissingSectorCommanders()
        {
            DataTable sectors = this.model.GetMissingSectorCommanders();
            List<string> msg = new List<string>();
            foreach (DataRow row in sectors.Rows)
            {
                msg.Add("Sector " + row["SectorName"].ToString() + " has no commander assigned");
            }

            return msg;
        }

        private void GetNotes()
        {
            DataTable notes = this.model.GetNotes(false);
            this.view.SetNotes(notes);
            DataTable gameNotes = this.model.GetNotes(true);
            this.view.SetGameNotes(gameNotes);
        }

        private List<string> GetObsoleteShips()
        {
            DataTable ships = this.model.GetObsoleteShips();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " has obsolete class: " + row["ClassName"].ToString());
            }

            return msg;
        }

        private List<string> GetObsoleteTooledShipyards()
        {
            DataTable shipyards = this.model.GetObsoleteTooledShipyards();
            List<string> msg = new List<string>();
            foreach (DataRow row in shipyards.Rows)
            {
                msg.Add("Shipyard " + row["ShipyardName"].ToString() + " on planet " + row["PopName"].ToString() + " is tooled for obsolete class: " + row["ClassName"].ToString());
            }

            return msg;
        }

        private void GetPopulations()
        {
            DataTable populations = this.model.GetPopulations();
            this.view.SetPopulations(populations);
        }

        private List<string> GetPopulationsWithoutGovernor()
        {
            DataTable populations = this.model.GetPopulationsWithoutGovernor();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                msg.Add("Population " + row["PopName"].ToString() + " has no governor assigned");
            }

            return msg;
        }

        private List<string> GetResearchFieldMismatch()
        {
            DataTable researcher = this.model.GetResearchFieldMismatch();
            List<string> msg = new List<string>();
            foreach (DataRow row in researcher.Rows)
            {
                msg.Add("Research field of " + row["Name"].ToString() + " on " + row["PopName"].ToString() + " working on " + row["Research"].ToString() + " does not match the research bonus");
            }

            return msg;
        }

        private List<string> GetResearchWithoutResearchFacility()
        {
            DataTable population = this.model.GetResearchWithoutResearchFacility();
            List<string> msg = new List<string>();
            foreach (DataRow row in population.Rows)
            {
                msg.Add("Population " + row["PopName"].ToString() + " has active research projects but no available research facility");
            }

            return msg;
        }

        private void GetResources()
        {
            this.view.SetResources(this.model.GetSavedResources());
            this.view.SetWealthPoints(this.model.GetSavedWealthPoints());
        }

        private List<string> GetSelfSustainingColonistDestination()
        {
            DataTable populations = this.model.GetSelfSustainingColonistDestination();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                msg.Add("Self sustaining colony " + row["PopName"].ToString() + " is set as a colonist destination");
            }

            return msg;
        }

        private void GetShips()
        {
            DataTable ships = this.model.GetShips();
            this.view.SetShips(ships);
        }

        private void GetEventColours()
        {
            DataTable events = this.model.GetEventColours();
            this.view.SetEventColours(events);
        }

        private List<string> GetShipsWithArmorDamage()
        {
            DataTable ships = this.model.GetShipsWithArmorDamage();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " has " + row["SUM(Damage)"].ToString() + " armor damage");
            }

            return msg;
        }

        private List<string> GetShipWithoutMsp()
        {
            DataTable ships = this.model.GetShipWithoutMsp();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " of fleet " + row["FleetName"].ToString() + " has no MSPs left");
            }

            return msg;
        }

        private List<string> GetShipWithLowMsp()
        {
            DataTable ships = this.model.GetShipWithLowMsp();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                msg.Add("Ship " + row["ShipName"].ToString() + " of fleet " + row["FleetName"].ToString() + " has only " + row["SupplyLevel"].ToString() + "% of MSPs left");
            }

            return msg;
        }

        private List<string> GetUnusedTerraformers()
        {
            DataTable populations = this.model.GetUnusedTerraformers();
            List<string> msg = new List<string>();
            foreach (DataRow row in populations.Rows)
            {
                msg.Add("Planet " + row["PopName"].ToString() + " has unused terraforming installations: " + row["Amount"].ToString());
            }

            return msg;
        }

        private List<string> GetWrecks()
        {
            DataTable ships = this.model.GetWrecks();
            List<string> msg = new List<string>();
            foreach (DataRow row in ships.Rows)
            {
                msg.Add("Wreck of ship class " + row["ClassName"].ToString() + " located in system " + row["Name"].ToString());
            }

            return msg;
        }
    }
}