using Microsoft.Xna.Framework;

namespace Lib;

//Events
internal class Event
{
    double delay;
    double startTime;
    static double globalTime;
    Action function;

    public Event(Action function, double delay)
    {
        this.delay = delay;
        this.function = function;
        startTime = globalTime;
    }

    static List<Event> events = new();

    static public void Add(Action func, double delay) => events.Add(new Event(func, delay));

    static public void ExecuteEvents(GameTime gameTime)
    {
        for (int i = 0; i < events.Count; ++i)
        {
            Event ev = events[i];
            if ((globalTime - ev.startTime) > ev.delay)
            {
                ev.function.Invoke();
                events.Remove(ev);
                --i;
            }
        }

        globalTime += gameTime.ElapsedGameTime.TotalSeconds;
    }

    static public void ClearEvents() => events.Clear();
}