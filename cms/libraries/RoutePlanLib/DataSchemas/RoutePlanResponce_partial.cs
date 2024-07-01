using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XSD.RoutePlanResponce;
using XSD.RoutePlan;

namespace XSD.RoutePlanResponce
{
	public partial class rcsMsg
	{
		public rcsMsg(string sender, string schema)
		{
			this.hdrField = new hdr(sender, schema);
			this.dataField = new data();
		}

		public rcsMsg()
		{

		}
	}

	public partial class hdr
	{
		public hdr(string sender, string schema)
		{
			this.senderField = sender;
			this.utcField = DateTime.Now.ToString("yyyyMMddThhmmss");
			this.schemaField = schema;
		}

		public hdr()
		{

		}
	}

	public partial class ActionPlanTrains
	{
		public ActionPlanTrains(XSD.RoutePlan.Train[] trains)
		{
			if ((trains?.Length ?? 0) == 0)
				return;

			this.trainField = new Train[trains.Length];

			int index = 0;
			foreach (var train in trains)
				trainField[index++] = new Train(train);
		}

		public ActionPlanTrains()
		{

		}
	}

	public partial class Train
	{
		public Train(XSD.RoutePlan.Train train)
		{
			this.ctcTrainIdField = train.CTCID;
			this.serviceNameField = train.SerN;
			this.TripID = train.TripID;
			this.pathsField = new TrainPaths(train.Items, train);
		}

		public Train()
		{

		}
	}

	public partial class TrainPaths
	{
		public TrainPaths(XSD.RoutePlan.Path[] items, XSD.RoutePlan.Train train)
		{
			if ((items?.Length ?? 0) == 0)
				return;
			
			this.Path = new Path[items.Length];

			for (int index = 0; index < items.Length; ++index)
			{
				if (index + 1 < items.Length)
					this.pathField[index] = new Path(items[index], items[index + 1], train);
				else
					this.pathField[index] = new Path(items[index], null, train);
			}
		}

		public TrainPaths()
		{

		}
	}

	public partial class Path
	{
		public Path(XSD.RoutePlan.Path path, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train)
		{
			this.idField = path.ID;
            this.TripIDSpecified = path.TrID != null && path.TrID != 0;
            if (this.TripIDSpecified)
                this.TripID = path.TrID;
			this.masterActionsField = new PathMasterActions(path.MasterRoute, path, nextPath, train);
		}

		public Path()
		{

		}
	}

	public partial class PathMasterActions
	{
		public PathMasterActions(XSD.RoutePlan.MasterRoute[] masterRoutes, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train)
		{
			if ((masterRoutes?.Length ?? 0) == 0)
				return;

			this.masterActionField = new MasterAction[masterRoutes.Length];
			int index = 0;
			foreach (var route in masterRoutes)
				this.masterActionField[index++] = new MasterAction(route, currPath, nextPath, train, route.Type);
		}

		public PathMasterActions()
		{

		}
	}

	public partial class MasterAction
	{
		public MasterAction(XSD.RoutePlan.MasterRoute route, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train, string type)
		{
			this.typeField = type;
			this.startField = route.start.ToString();
			this.endField = route.end.ToString();
			if (route?.Actions?.Length == 0)
				return;

			List<BaseAction> newActions = new List<BaseAction>();

			foreach (var action in route.Actions)
			{
				if (action is XSD.RoutePlan.RCA rca)
					newActions.Add(new RCA(rca, currPath, nextPath, train));
				else if (action is XSD.RoutePlan.SCA sca)
					newActions.Add(new SCA(sca, currPath, nextPath, train));
				else if (action is XSD.RoutePlan.SCRA scra)
					newActions.Add(new SCRA(scra, currPath, nextPath, train));
				else if (action is XSD.RoutePlan.ITA ita)
					newActions.Add(new ITA(ita, currPath, nextPath, train));
			}

			this.actionsField = newActions.ToArray(); // new BaseAction[route.Actions.Length];  new MasterActionActions(route.Actions, currPath, nextPath, train);
		}

		public MasterAction()
		{
			this.typeField = "";
			this.startField = "";
			this.endField = "";
		}
	}

	public partial class BaseAction
	{
		public BaseAction(XSD.RoutePlan.BaseAction baseAction, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train)
		{
			ApplyCommonProperties(baseAction, currPath, nextPath, train);
			this.commandField = GenerateCommands(baseAction, currPath, nextPath, train);
		}

		private void ApplyCommonProperties(XSD.RoutePlan.BaseAction baseAction, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train)
		{
			this.seqnoField = baseAction.SeqNo;
			this.actionPointField = baseAction.Obj.ename;
			this.timingModeField = baseAction.TimingMode;
			this.secsField = baseAction.Obj.secs.ToString();

			switch (baseAction.TimingMode)
			{
				case 1: this.executionTimeField = string.IsNullOrEmpty(currPath.From?.RA) ? currPath.From?.PA ?? String.Empty : currPath.From.RA; break;
				case 2: this.executionTimeField = string.IsNullOrEmpty(currPath.From?.RD) ? currPath.From?.PD ?? String.Empty : currPath.From.RD; break;
				case 3: this.executionTimeField = currPath.From?.PA ?? String.Empty; break;
				case 4: this.executionTimeField = currPath.From?.PD ?? String.Empty; break;
				case 5: this.executionTimeField = string.IsNullOrEmpty(currPath.To?.RA) ? currPath.To?.PA ?? String.Empty : currPath.To.RA; break;
				case 6: this.executionTimeField = string.IsNullOrEmpty(currPath.To?.RD) ? currPath.To?.PD ?? String.Empty : currPath.To.RD; break;
				case 7: this.executionTimeField = currPath.To?.PA ?? String.Empty; break;
				case 8: this.executionTimeField = currPath.To?.PD ?? String.Empty; break;
				case 9:
				default:
					this.executionTimeField = ""; break;
			}
		}

		protected virtual Command[] GenerateCommands(RoutePlan.BaseAction baseAction, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path prevPath, XSD.RoutePlan.Train train)
		{
			List<Command> tmpCommands = new List<Command>();
			if (baseAction.Command != null)
			{
				foreach (var command in baseAction.Command)
					tmpCommands.Add(new Command(command));
			}
			return tmpCommands.ToArray();
		}
	}

	public partial class RCA
	{
		public RCA(XSD.RoutePlan.RCA rca, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train) : base(rca, currPath, nextPath, train) {}
		public RCA() { }
	}

	public partial class SCA
	{
		public SCA(XSD.RoutePlan.SCA sca, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train) : base(sca, currPath, nextPath, train) { }
		public SCA() { }
	}

	public partial class ITA
	{
		public ITA(XSD.RoutePlan.ITA ita, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train) : base(ita, currPath, nextPath, train) { }
		public ITA() { }
	}

	public partial class SCRA
	{
		public SCRA(XSD.RoutePlan.SCRA scra, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train) : base(scra, currPath, nextPath, train) { }
		public SCRA() { }

		protected override Command[] GenerateCommands(RoutePlan.BaseAction baseAction, XSD.RoutePlan.Path currPath, XSD.RoutePlan.Path nextPath, XSD.RoutePlan.Train train)
		{
			List<string> cmds = new List<string>() {
			"ROUTE_DEST",
			"TRIP_ORIGIN",
			"TRIP_DEST",
			"FIRST_PASSENGER_STOP",
			"LAST_PASSENGER_STOP",
			"ARRIVAL_TIME",
			"DEPARTURE_TIME",
			"NEXT_ARRIVAL_TIME",
			"NEXT_DEPARTURE_TIME",
			"STOPPING_1",
			"NEXT_DEST_1",
			"STOPPING_2",
			"NEXT_DEST_2"};

			List<Command> tmpCommands = base.GenerateCommands(baseAction, currPath, nextPath, train).ToList();

			tmpCommands.Add(new Command(cmds[0], currPath.MasterRoute[0].destination, ""));
			tmpCommands.Add(new Command(cmds[1], train.Origin, ""));
			tmpCommands.Add(new Command(cmds[2], train.Destination, ""));
			tmpCommands.Add(new Command(cmds[3], train.FirstPassengerStop, ""));
			tmpCommands.Add(new Command(cmds[4], train.LastPassengerStop, ""));
			tmpCommands.Add(new Command(cmds[5], string.IsNullOrEmpty(currPath.From.RA) ? currPath.From.PA : currPath.From.RA, ""));
			tmpCommands.Add(new Command(cmds[6], string.IsNullOrEmpty(currPath.From.RD) ? currPath.From.PD : currPath.From.RD, ""));
			tmpCommands.Add(new Command(cmds[7], string.IsNullOrEmpty(currPath.To.RA) ? currPath.To.PA : currPath.To.RA, ""));
			tmpCommands.Add(new Command(cmds[8], string.IsNullOrEmpty(currPath.To.RD) ? currPath.To.PD : currPath.To.RD, ""));

			tmpCommands.Add(new Command(cmds[9], currPath.MasterRoute[0].destination, (currPath.Platform.stop ? "1" : "0")));
			tmpCommands.Add(new Command(cmds[10], currPath.MasterRoute[0].destination, ""));

			if (nextPath != null)
			{
				tmpCommands.Add(new Command(cmds[11], nextPath.MasterRoute[0].destination, (nextPath.Platform.stop ? "1" : "0")));
				tmpCommands.Add(new Command(cmds[12], nextPath.MasterRoute[0].destination, ""));
			}
			else
			{
				tmpCommands.Add(new Command(cmds[11], "", ""));
				tmpCommands.Add(new Command(cmds[12], "", ""));
			}

			return tmpCommands.ToArray();
		}
	}

	public partial class Command
	{
		public Command(XSD.RoutePlan.Command command)
		{
			this.targetField = command.value;
			this.cmdField = command.cmd;

			if ((command?.Properties?.Length ?? 0) == 0)
				return;

			this.propertiesField = new Properties(command.Properties);
		}

		public Command(string cmd, string target, string value)
		{
			this.cmdField = cmd;
			this.targetField = target;
			this.valueField = value;
		}

		public Command()
		{

		}
	}

	public partial class Properties
	{
		public Properties(XSD.RoutePlan.Property[] properties)
		{
			this.propertyField = new Property[properties.Length];

			int index = 0;
			foreach (var prop in properties)
				this.propertyField[index++] = new Property(prop);
		}

		public Properties()
		{

		}
	}

	public partial class Property
	{
		public Property(XSD.RoutePlan.Property prop)
		{
			this.conditionField = prop.condition;
			this.testField = prop.test;
			this.valueField = prop.value;
		}

		public Property()
		{

		}
	}

}
