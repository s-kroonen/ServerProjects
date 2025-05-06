using BeerTap.Models;

namespace BeerTap.Services
{
    public class TapQueueManager
    {
        private readonly Dictionary<string, Queue<TapQueueEntry>> _tapQueues = new();
        private readonly object _lock = new();

        public event Action<string, string>? CurrentUserChanged;
        public void EnqueueUser(string tapId, string userId)
        {
            lock (_lock)
            {
                if (!_tapQueues.ContainsKey(tapId))
                    _tapQueues[tapId] = new Queue<TapQueueEntry>();

                // Avoid duplicates
                if (!_tapQueues[tapId].Any(q => q.UserId == userId))
                {
                    _tapQueues[tapId].Enqueue(new TapQueueEntry
                    {
                        UserId = userId,
                        TapId = tapId,
                        QueuedAt = DateTime.UtcNow
                    });
                    Console.WriteLine($"User {userId} enqueued for tap {tapId}");
                }
            }
        }
        public TapQueueEntry? DequeueUser(string tapId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue) && queue.Count > 0)
                {
                    var user = queue.Dequeue();

                    if (queue.Count > 0)
                    {
                        var nextUser = queue.Peek();
                        CurrentUserChanged?.Invoke(tapId, nextUser.UserId);
                    }
                    else
                    {
                        // No one left in the queue
                        CurrentUserChanged?.Invoke(tapId, "");
                    }

                    return user;
                }

                return null;
            }
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
                    var list = queue.ToList(); // Snapshot to prevent enumeration issues
                    return list.FindIndex(entry => entry.UserId == userId) + 1;
                }

                return -1;
            }
        }

        public void Cancel(string tapId, string userId)
        {
            lock (_lock)
            {
                if (_tapQueues.TryGetValue(tapId, out var queue))
                {
                    // User must not be first in line (currently tapping)
                    if (queue.Count > 0 && queue.Peek().UserId != userId)
                    {
                        var updatedQueue = new Queue<TapQueueEntry>(
                            queue.Where(entry => entry.UserId != userId)
                        );

                        _tapQueues[tapId] = updatedQueue;
                    }
                }
            }
        }

        public bool HasUsers(string tapId)
        {
            lock (_lock)
            {
                return _tapQueues.TryGetValue(tapId, out var queue) && queue.Any();
            }
        }
    }

}
