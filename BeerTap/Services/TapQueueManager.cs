using BeerTap.Models;
using Microsoft.AspNetCore.SignalR;
using BeerTap.Hubs;

namespace BeerTap.Services
{
    public class TapQueueManager
    {
        private readonly Dictionary<string, Queue<TapQueueEntry>> _tapQueues = new();
        private readonly object _lock = new();
        private readonly IHubContext<TapQueueHub> _hubContext;

        public event Action<string, string>? CurrentUserChanged;

        public TapQueueManager(IHubContext<TapQueueHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task EnqueueUser(string tapId, string userId)
        {
            bool notify = false;

            lock (_lock)
            {
                if (!_tapQueues.ContainsKey(tapId))
                    _tapQueues[tapId] = new Queue<TapQueueEntry>();

                if (!_tapQueues[tapId].Any(q => q.UserId == userId))
                {
                    _tapQueues[tapId].Enqueue(new TapQueueEntry
                    {
                        UserId = userId,
                        TapId = tapId,
                        QueuedAt = DateTime.UtcNow
                    });

                    Console.WriteLine($"User {userId} enqueued for tap {tapId}");

                    if (_tapQueues[tapId].Count == 1)
                    {
                        var currentUser = _tapQueues[tapId].Peek();
                        CurrentUserChanged?.Invoke(tapId, currentUser.UserId);
                    }

                    notify = true;
                }
            }

            if (notify)
                await NotifyQueueChangedAsync(tapId);
        }

        public async Task<TapQueueEntry?> DequeueUser(string tapId)
        {
            TapQueueEntry? user = null;
            bool notify = false;

            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Count > 0)
                {
                    user = queue.Dequeue();
                    Console.WriteLine($"User {user.UserId} dequeued for tap {tapId}");

                    if (queue.Count > 0)
                    {
                        var nextUser = queue.Peek();
                        CurrentUserChanged?.Invoke(tapId, nextUser.UserId);
                    }
                    else
                    {
                        CurrentUserChanged?.Invoke(tapId, "");
                    }

                    notify = true;
                }
            }

            if (notify)
                await NotifyQueueChangedAsync(tapId);

            return user;
        }

        public bool IsUserNext(string tapId, string userId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Count > 0)
                {
                    return queue.Peek().UserId == userId;
                }

                return false;
            }
        }

        public TapQueueEntry? PeekCurrentUser(string tapId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Any())
                    return queue.Peek();
                return null;
            }
        }

        public int GetUserPosition(string tapId, string userId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                {
                    var list = queue.ToList();
                    return list.FindIndex(entry => entry.UserId == userId);
                }

                return -1;
            }
        }

        public async Task Cancel(string tapId, string userId)
        {
            bool notify = false;

            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                {
                    if (queue.Count > 0 && queue.Peek().UserId != userId)
                    {
                        var updatedQueue = new Queue<TapQueueEntry>(
                            queue.Where(entry => entry.UserId != userId)
                        );

                        _tapQueues[tapId] = updatedQueue;
                        Console.WriteLine($"User {userId} cancelled for tap {tapId}");
                        notify = true;
                    }
                    else if (queue.Count > 0)
                    {
                        // Dequeue current user if they cancel
                        queue.Dequeue();
                        Console.WriteLine($"User {userId} dequeued via cancel for tap {tapId}");
                        notify = true;

                        if (queue.Count > 0)
                        {
                            var nextUser = queue.Peek();
                            CurrentUserChanged?.Invoke(tapId, nextUser.UserId);
                        }
                        else
                        {
                            CurrentUserChanged?.Invoke(tapId, "");
                            
                        }
                    }
                }
            }

            if (notify)
                await NotifyQueueChangedAsync(tapId);
        }

        public bool HasUsers(string tapId)
        {
            lock (_lock)
            {
                return _tapQueues.TryGetValue(tapId, out var queue) && queue.Any();
            }
        }

        public List<TapQueueEntry> GetQueueSnapshot(string tapId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                    return queue.ToList();
                return new List<TapQueueEntry>();
            }
        }

        private async Task NotifyQueueChangedAsync(string tapId)
        {
            var snapshot = GetQueueSnapshot(tapId);
            await _hubContext.Clients.Group(tapId).SendAsync("QueueUpdated", tapId, snapshot);
        }
    }
}
