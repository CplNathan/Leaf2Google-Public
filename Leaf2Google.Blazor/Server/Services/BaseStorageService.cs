﻿// Copyright (c) Nathan Ford. All rights reserved. BaseStorageManager.cs

using Leaf2Google.Entities.Car;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Car.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Leaf2Google.Services
{
    public interface IUserStorage
    {
        Task<bool> CanUserLogin(Guid sessionId);

        Task<Guid> DoCredentialsMatch(string username, string password, bool includeDeleted = false);

        Task<Guid> LoginUser(string username, string password);

        Task<bool> DeleteUser(Guid sessionId);

        Task<CarEntity?> RestoreUser(Guid sessionId);

        Task<CarEntity?> GetUser(Guid sessionId, bool includeDeleted = false);

        Task<CarEntity?> GetUser(string username, string password, bool includeDeleted = false);

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

        public async Task<Guid> DoCredentialsMatch(string username, string password, bool includeDeleted = false)
        {
            return (await GetUser(username, password, includeDeleted))?.CarModelId ?? Guid.Empty;
        }

        public async Task<Guid> LoginUser(string username, string password)
        {
            Guid sessionId = await DoCredentialsMatch(username, password);
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

        public async Task<CarEntity?> RestoreUser(Guid sessionId)
        {
            var leaf = await GetUser(sessionId, true);
            if (leaf == null)
                return leaf;

            leaf.Deleted = null;
            LeafContext.Entry(leaf).State = EntityState.Modified;
            await LeafContext.SaveChangesAsync();

            return leaf;
        }

        public async Task<CarEntity?> GetUser(string username, string password, bool includeDeleted = false)
        {
            var leafs = includeDeleted ? LeafContext.NissanLeafs.IgnoreQueryFilters() : LeafContext.NissanLeafs;

            return (await leafs.Where(car => car.NissanUsername == username).ToListAsync())
                .FirstOrDefault(car => car.NissanUsername == username && car.NissanPassword == password);
        }

        public async Task<CarEntity?> GetUser(Guid sessionId, bool includeDeleted = false)
        {
            var leafs = includeDeleted ? LeafContext.NissanLeafs.IgnoreQueryFilters() : LeafContext.NissanLeafs;

            return await leafs.FirstOrDefaultAsync(car => car.CarModelId == sessionId);
        }

        public Task<bool> CreateUser(string username, string password)
        {
            throw new NotImplementedException();
        }
    }

    public class SessionStorageContainer
    {
        public Dictionary<Guid, VehicleSessionBase> VehicleSessions { get; init; }

        public Dictionary<Guid, Dictionary<Type, BaseDeviceModel>> GoogleSessions { get; init; }

        public SessionStorageContainer()
        {
            VehicleSessions = new Dictionary<Guid, VehicleSessionBase>();
            GoogleSessions = new Dictionary<Guid, Dictionary<Type, BaseDeviceModel>>();
        }
    }

    public class BaseStorageService
    {
        public Guid ApplicationSecret { get; } = Guid.NewGuid();

        protected LeafContext LeafContext { get; }

        protected SessionStorageContainer StorageContainer { get; }

        public IUserStorage UserStorage { get; }

        public Dictionary<Guid, VehicleSessionBase> VehicleSessions { get => StorageContainer.VehicleSessions; }

        public Dictionary<Guid, Dictionary<Type, BaseDeviceModel>> GoogleSessions { get => StorageContainer.GoogleSessions; }

        public BaseStorageService(LeafContext leafContext, IUserStorage userStorage, SessionStorageContainer sessionStorageContainer)
        {
            LeafContext = leafContext;
            UserStorage = userStorage;

            StorageContainer = sessionStorageContainer;
        }
    }
}
