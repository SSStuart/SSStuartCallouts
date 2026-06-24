using Rage;
using System;
using System.Collections.Generic;

namespace SSStuartCallouts
{
    internal class CalloutTask
    {
        private readonly string _name;
        private bool _completed;
        private readonly bool _tracked;

        internal CalloutTask(string name, bool isTracked)
        {
            _name = name;
            _completed = false;
            _tracked = isTracked;
        }

        internal void UpdateStatus(bool completed)
        {
            _completed = completed;
        }

        internal new string ToString()
        {
            return (_tracked ? (_completed ? "~g~~BLIP_COMPLETED~" : "~BLIP_DARTS~") : "~BLIP_LEVEL~") + $" {_name}~w~~s~";
        }
    }

    public class Checklist
    {
        private bool active;
        private List<CalloutTask> tasks;
        private uint displayTimeout;

        public Checklist(bool activate = false)
        {
            active = activate;
            tasks = new List<CalloutTask>();
            displayTimeout = 0;
        }

        public int AddTask(string name, bool isTracked = true)
        {
            CalloutTask task = new CalloutTask(name, isTracked);
            tasks.Add(task);
            return tasks.IndexOf(task);
        }

        public void Enable()
        {
            active = true;
        }
        public void Disable()
        {
            active = false;
        }

        public void UpdateTaskStatus(int taskHandle, bool completed)
        {
            tasks[taskHandle].UpdateStatus(completed);
            Display(true);
        }

        public void Display(bool force = false)
        {
            if (force || (active && tasks.Count > 0 && displayTimeout + 10000 < Game.GameTime && !Game.LocalPlayer.Character.IsInAnyVehicle(false)))
                Game.DisplayHelp(this.ToString());
        }

        public new string ToString()
        {
            displayTimeout = Game.GameTime;

            List<string> tasksList = new List<string>();
            foreach (CalloutTask task in tasks)
            {
                tasksList.Add(task.ToString());
            }

            return String.Join("~n~", tasksList) + "~n~Press ~b~End~w~ to end the callout.";
        }
    }
}
