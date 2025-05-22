using BeerTap.Models;
using Microsoft.AspNetCore.SignalR;
using BeerTap.Hubs;

namespace BeerTap.Services
{
    public class TapQueueManager
    {
        private readonly ILogger<TapQueueManager> _logger;
        private readonly Dictionary<Guid, Queue<TapQueueEntry>> _tapQueues = new();
        private readonly object _lock = new();
        //private readonly IHubContext<TapQueueHub> _hubContext;

        public event Action<Guid, User>? CurrentUserChanged;
        public event Action<Guid>? StopTapSession;

        public TapQueueManager(IHubContext<TapQueueHub> hubContext, ILogger<TapQueueManager> logger)
        {
            //_hubContext = hubContext;
            _logger = logger;
        }

        public async Task EnqueueUser(Guid tapId, User user)
        {
            bool notify = false;

            lock (_lock)
            {
                if (!_tapQueues.ContainsKey(tapId))
                    _tapQueues[tapId] = new Queue<TapQueueEntry>();

                if (!_tapQueues[tapId].Any(q => q.User.ID == user.ID))
                {
                    _tapQueues[tapId].Enqueue(new TapQueueEntry
                    {
                        User = user,
                        TapId = tapId,
                        QueuedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation($"User {user.UserId} enqueued for tap {tapId}");

                    if (_tapQueues[tapId].Count == 1)
                    {
                        var currentQueue = _tapQueues[tapId].Peek();
                        CurrentUserChanged?.Invoke(tapId, currentQueue.User);
                    }

                    notify = true;
                }
            }

            if (notify)
                await NotifyQueueChangedAsync(tapId);
        }

        public async Task<TapQueueEntry?> DequeueUser(Guid tapId)
        {
            TapQueueEntry? user = null;
            bool notify = false;

            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Count > 0)
                {
                    user = queue.Dequeue();
                    _logger.LogInformation($"User {user.User.UserId} dequeued for tap {tapId}");

                    if (queue.Count > 0)
                    {
                        var nextQueue = queue.Peek();
                        CurrentUserChanged?.Invoke(tapId, nextQueue.User);
                    }
                    else
                    {
                        CurrentUserChanged?.Invoke(tapId, null);
                    }

                    notify = true;
                }
            }

            if (notify)
                await NotifyQueueChangedAsync(tapId);

            return user;
        }

        public async Task DequeueUserFromAllTaps(User user)
        {
            var tapsToNotify = new List<Guid>();

            lock (_lock)
            {
                foreach (var kvp in _tapQueues)
                {
                    var tapId = kvp.Key;
                    var queue = kvp.Value;

                    // Filter out all entries for the given user
                    var originalCount = queue.Count;
                    var filteredQueue = new Queue<TapQueueEntry>(queue.Where(entry => entry.User.ID != user.ID));
                    if (filteredQueue.Count != originalCount)
                    {
                        StopTapSession?.Invoke(tapId);
                        _tapQueues[tapId] = filteredQueue;
                        tapsToNotify.Add(tapId);
                        _logger.LogInformation($"User {user} dequeued from tap {tapId}");
                    }

                    // Fire event for current user change if needed
                    if (queue.Count > 0)
                    {
                        var currentUser = filteredQueue.Count > 0 ? filteredQueue.Peek().User : null;
                        CurrentUserChanged?.Invoke(tapId, currentUser);
                    }
                }
            }

            foreach (var tapId in tapsToNotify)
            {
                await NotifyQueueChangedAsync(tapId);
            }
        }



        public bool IsUserNext(Guid tapId, User user)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Count >= 0)
                {
                    return queue.Peek().User.ID == user.ID;
                }

                return false;
            }
        }

        public TapQueueEntry? PeekCurrentUser(Guid tapId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Any())
                    return queue.Peek();
                return null;
            }
        }

        public int GetUserPosition(Guid tapId, User user)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                {
                    var list = queue.ToList();
                    return list.FindIndex(entry => entry.User.ID == user.ID);
                }

                return -1;
            }
        }

        public async Task Cancel(Guid tapId, User user)
        {
            bool notify = false;

            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                {
                    if (queue.Count > 0 && queue.Peek().User.ID != user.ID)// Check if user is first in queue then only cancel
                    {
                        var updatedQueue = new Queue<TapQueueEntry>(
                            queue.Where(entry => entry.User.ID != user.ID)
                        );

                        _tapQueues[tapId] = updatedQueue;
                        _logger.LogInformation($"User {user.UserId} cancelled for tap {tapId}");
                        
                        notify = true;
                    }
                    else if (queue.Count > 0)// Check if queue is full then stop tapping
                    {
                        StopTapSession?.Invoke(tapId);
                        // Dequeue current user if they cancel
                        queue.Dequeue();
                        _logger.LogInformation($"User {user.UserId} dequeued via cancel for tap {tapId}");
                        notify = true;

                        if (queue.Count > 0)
                        {
                            var nextUser = queue.Peek();
                            CurrentUserChanged?.Invoke(tapId, nextUser.User);
                        }
                        else
                        {
                            CurrentUserChanged?.Invoke(tapId, null);
                            
                        }
                    }
                }
            }

            if (notify)
                await NotifyQueueChangedAsync(tapId);
        }

        public bool HasUsers(Guid tapId)
        {
            lock (_lock)
            {
                return _tapQueues.TryGetValue(tapId, out var queue) && queue.Any();
            }
        }

        public List<TapQueueEntry> GetQueueSnapshot(Guid tapId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                    return queue.ToList();
                return new List<TapQueueEntry>();
            }
        }

        private async Task NotifyQueueChangedAsync(Guid tapId)
        {
            var snapshot = GetQueueSnapshot(tapId);
            //await _hubContext.Clients.Group(tapId).SendAsync("QueueUpdated", tapId, snapshot);
        }
    }
}
