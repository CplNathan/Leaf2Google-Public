using Leaf2Google.Contexts;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Leaf;
using Leaf2Google.Models.Nissan;
using Microsoft.EntityFrameworkCore;

namespace Leaf2Google.Dependency.Managers
{
    public interface ILeafSessionManager
    {
        Task StartAsync();

        Task AddAsync(Leaf NewLeaf);
    }

    public class LeafSessionManager : ILeafSessionManager, IDisposable
    {
        private readonly HttpClient _client;

        protected HttpClient Client { get => _client; }

        private readonly IServiceScopeFactory _serviceScopeFactory;

        private List<NissanConnectSession> _leafSessions = new List<NissanConnectSession>();

        public List<NissanConnectSession> LeafSessions { get => _leafSessions; }

        private List<NissanConnectSession> _addSessionQueue = new List<NissanConnectSession>();

        private Timer? _timer;

        public LeafSessionManager(HttpClient client, IServiceScopeFactory serviceScopeFactory)
        {
            _client = client;
            _serviceScopeFactory = serviceScopeFactory;
        }

        private async void LeafSessionRunAsync(object? state)
        {
            _leafSessions = _leafSessions.Concat(_addSessionQueue).ToList();
            _addSessionQueue.Clear();

            using var scope = _serviceScopeFactory.CreateScope();
            var _leafContext = scope.ServiceProvider.GetRequiredService<LeafContext>();
            var _googleState = scope.ServiceProvider.GetRequiredService<GoogleStateManager>();

            bool dbChanges = false;

            // Load queued sessions into memory and authenticate.
            foreach (var session in _leafSessions.Where(session => !session.Authenticated && !session.LastRequestSuccessful).ToList())
            {
                dbChanges = true;

                bool success = false;
                string auditLog = string.Empty;

                Console.WriteLine("Authenticating");
                try
                {
                    success = await session.Login();
                }
                catch (Exception ex)
                {
                    auditLog = ex.ToString();
                }
                finally
                {
                    Console.WriteLine(auditLog);
                }

                if (!success)
                {
                    Console.WriteLine($"{session.Username} - Authentication Failed {(string.IsNullOrEmpty(auditLog) ? "" : $" - {auditLog}")}");
                    _leafContext.NissanAudits.Add(new Audit<Leaf>
                    {
                        Owner = _leafContext.NissanLeafs.FirstOrDefault(leaf => leaf.NissanUsername == session.Username),
                        Action = AuditAction.Access,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Authentication Failed {(string.IsNullOrEmpty(auditLog) ? "" : $" - {auditLog}")}",
                    });
                }
                else
                {
                    Console.WriteLine($"{session.Username} - Authentication Success");
                    _leafContext.NissanAudits.Add(new Audit<Leaf>
                    {
                        Owner = _leafContext.NissanLeafs.FirstOrDefault(leaf => leaf.NissanUsername == session.Username),
                        Action = AuditAction.Access,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Authentication Success",
                    });

                    _googleState.GetOrCreateDevices(session.SessionId);
                }

                if (success)
                {
                    _leafContext.NissanLeafs.First(leaf => leaf.LeafId == session.SessionId).PrimaryVin = string.IsNullOrEmpty(session.PrimaryVin) ? session.VINs.FirstOrDefault() ?? string.Empty : session.PrimaryVin;
                }
            }

            // Delete stale sessions and leafs.
            var failedSessions = _leafSessions.Where(session => !HasAuthenticated(session.SessionId) && HasGivenUp(session.SessionId)).ToList();
            foreach (var failedSession in failedSessions)
            {
                dbChanges = true;

                _leafSessions.Remove(failedSession);

                var foundLeaf = _leafContext.NissanLeafs.FirstOrDefault(leaf => leaf.LeafId == failedSession.SessionId);
                if (foundLeaf != null)
                {
                    foundLeaf.Deleted = DateTime.UtcNow;
                    _leafContext.Entry(foundLeaf).State = EntityState.Modified;
                }

                _leafContext.NissanAudits.Add(new Audit<Leaf>
                {
                    Owner = foundLeaf,
                    Action = AuditAction.Delete,
                    Context = AuditContext.Account,
                    Data = $"{failedSession.Username} - Deleting Stale Leaf",
                });
            }

            if (dbChanges)
                await _leafContext.SaveChangesAsync();
        }

        public Task StartAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var _leafContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

            // Queue saved sessions into memory.
            foreach (var leaf in _leafContext.NissanLeafs.Where(leaf => leaf.Deleted == null))
            {
                var session = new NissanConnectSession(Client, leaf.NissanUsername, leaf.NissanPassword, leaf.LeafId, leaf.PrimaryVin);
                _addSessionQueue.Add(session);
            }

            _timer = new Timer(LeafSessionRunAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public async Task AddAsync(Leaf NewLeaf)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var _leafContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

            var session = new NissanConnectSession(Client, NewLeaf.NissanUsername, NewLeaf.NissanPassword, NewLeaf.LeafId);
            _addSessionQueue.Add(session);

            _leafContext.NissanAudits.Add(new Audit<Leaf>
            {
                Owner = null,
                Action = AuditAction.Create,
                Context = AuditContext.Account,
                Data = $"{session.Username} - Adding New Leaf",
            });

            var authenticated = false;
            var givenUp = false;
            while (!authenticated && !givenUp)
            {
                givenUp = HasGivenUp(NewLeaf.LeafId);
                authenticated = HasAuthenticated(NewLeaf.LeafId);
                if (authenticated)
                {
                    _leafContext.NissanLeafs.Add(NewLeaf);
                }

                await Task.Delay(250);
            }

            await _leafContext.SaveChangesAsync();
        }

        public bool HasGivenUp(Guid sessionId)
        {
            var foundSession = _leafSessions.Concat(_addSessionQueue).FirstOrDefault(session => session.SessionId == sessionId);

            if (foundSession != null)
            {
                if (foundSession.Authenticated)
                    return false;

                if (foundSession.LoginFailedCount >= 5)
                    return true;

                return false;
            }
            else
            {
                return true;
            }
        }

        public bool HasAuthenticated(Guid sessionId)
        {
            var foundSession = _leafSessions.Concat(_addSessionQueue).FirstOrDefault(session => session.SessionId == sessionId);

            if (foundSession != null)
            {
                return foundSession.Authenticated;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}