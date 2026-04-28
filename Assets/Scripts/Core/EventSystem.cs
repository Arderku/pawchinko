using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Project-wide generic publish/subscribe bus. Distinct from UnityEngine.EventSystems.EventSystem
    /// (which handles UI input). Managers subscribe in Initialize and unsubscribe in OnDestroy.
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        private static EventSystem _instance;

        public static EventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameEventSystem");
                    _instance = go.AddComponent<EventSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<Type, List<object>> _listeners = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Subscribes a callback to all events of type T.
        /// </summary>
        public void Subscribe<T>(Action<T> cb) where T : class
        {
            var t = typeof(T);
            if (!_listeners.TryGetValue(t, out var list))
            {
                list = new List<object>();
                _listeners[t] = list;
            }
            list.Add(cb);
        }

        /// <summary>
        /// Removes a previously-registered callback.
        /// </summary>
        public void Unsubscribe<T>(Action<T> cb) where T : class
        {
            if (_listeners.TryGetValue(typeof(T), out var list))
            {
                list.Remove(cb);
            }
        }

        /// <summary>
        /// Synchronously dispatches an event to every subscribed listener.
        /// </summary>
        public void Publish<T>(T evt) where T : class
        {
            if (!_listeners.TryGetValue(typeof(T), out var list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                (list[i] as Action<T>)?.Invoke(evt);
            }
        }
    }
}
