namespace Notcomd.Evenbus
{
    public class SubscriptionsManager
    {


        private readonly Dictionary<string, List<Type>> _handlers = new Dictionary<string, List<Type>>();


        public event EventHandler<string> OnEventRemoved;


        public bool IsEmpty => !_handlers.Keys.Any();


        public void Clear() => _handlers.Clear();


        public void AddSubscription(string eventName, Type eventHandlerType)
        {
            if (!HasSubscriptionForEvent(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }
            if (_handlers[eventName].Contains(eventHandlerType))
            {
                throw new ArgumentNullException($"");
            }
            _handlers[eventName].Add(eventHandlerType);
        }


        public void RemoveSubscription(string eventName, Type eventHandlerType)
        {
            _handlers[eventName].Remove(eventHandlerType);
            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
                OnEventRemoved.Invoke(this, eventName);
            }
        }


        public IEnumerable<Type> GetHandlersForEvent(string eventName) => _handlers[eventName];


        public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);

    }
}
