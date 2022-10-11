// Copyright (c) Nathan Ford. All rights reserved. BaseStorageManager.cs

using Leaf2Google.Models.Car;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Leaf2Google.Dependency
{
    public interface IUserStorage
    {
        Task<bool> CanUserLogin(Guid sessionId);

        Task<Guid?> DoCredentialsMatch(string username, string password);

        Task<Guid> LoginUser(string username, string password);

        Task<bool> DeleteUser(Guid sessionId);

        Task<CarModel?> RestoreUser(Guid sessionId);

        Task<CarModel?> GetUser(Guid sessionId);

        Task<bool> CreateUser(string username, string password);

       // Task<NissanConnectSession>
    }

    public class UserStorage : IUserStorage
    {
        protected LeafContext LeafContext { get; }

        public UserStorage(LeafContext leafContext)
        {
            LeafContext = leafContext;
        }

        public async Task<bool> CanUserLogin(Guid sessionId)
        {
            return await LeafContext.NissanLeafs.AnyAsync(car => car.Deleted == null && car.CarModelId == sessionId);
        }

        public async Task<Guid?> DoCredentialsMatch(string username, string password)
        {
            return (await LeafContext.NissanLeafs.ToListAsync()).FirstOrDefault(car => car.Deleted == null && car.NissanUsername == username && car.NissanPassword == password)?.CarModelId;
        }

        public async Task<Guid> LoginUser(string username, string password)
        {
            Guid sessionId = await DoCredentialsMatch(username, password) ?? Guid.Empty;
            if (sessionId == Guid.Empty)
                return Guid.Empty;

            bool canUserLogin = await CanUserLogin(sessionId);
            if (!canUserLogin)
                return Guid.Empty;

            return sessionId;
        }

        public async Task<bool> DeleteUser(Guid sessionId)
        {
            var leaf = await GetUser(sessionId);
            if (leaf == null)
                return false;

            leaf.Deleted = DateTime.UtcNow;

            LeafContext.Entry(leaf).State = EntityState.Modified;
            await LeafContext.SaveChangesAsync();

            return true;
        }

        public async Task<CarModel?> RestoreUser(Guid sessionId)
        {
            var leaf = await GetUser(sessionId);
            if (leaf == null)
                return leaf;

            leaf.Deleted = null;
            LeafContext.Entry(leaf).State = EntityState.Modified;
            await LeafContext.SaveChangesAsync();

            return leaf;
        }

        public async Task<CarModel?> GetUser(Guid sessionId)
        {
            return await LeafContext.NissanLeafs.FirstOrDefaultAsync(car => car.CarModelId == sessionId);
        }

        public Task<bool> CreateUser(string username, string password)
        {
            throw new NotImplementedException();
        }
    }

    public class BaseStorageManager
    {
        protected LeafContext LeafContext { get; }
        public IUserStorage UserStorage { get; }

        public BaseStorageManager(LeafContext leafContext, IUserStorage userStorage)
        {
            LeafContext = leafContext;
            UserStorage = userStorage;
        }
    }
}
