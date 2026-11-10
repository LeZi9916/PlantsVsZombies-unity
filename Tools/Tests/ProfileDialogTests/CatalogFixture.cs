using System;
using System.Collections.Generic;
using System.IO;

namespace PlantsVsZombies.Profiles
{
    public sealed class UserProfile
    {
        public Guid Id { get; internal set; }
        public string Name { get; internal set; }
        public int AdventureLevel { get; internal set; }
    }

    // A controllable test double for the catalog's public API.
    public sealed class ProfileCatalog
    {
        private readonly List<UserProfile> items = new List<UserProfile>();
        public string FailOperation;
        public string NoopOperation;
        public string CommitThenThrow;
        public bool FailRead;
        public int CreateCalls, RenameCalls, SelectCalls, DeleteCalls;
        public ProfileCatalog(string path) { }
        public IReadOnlyList<UserProfile> Profiles
        {
            get { if (FailRead) throw new IOException("读取失败"); return items.AsReadOnly(); }
        }
        public Guid? SelectedId
        {
            get { if (FailRead) throw new IOException("读取失败"); return selectedId; }
        }
        private Guid? selectedId;
        public void Create(string name)
        {
            CreateCalls++;
            Before("create");
            if (NoopOperation == "create") return;
            foreach (UserProfile item in items)
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("用户名已存在");
            UserProfile created = new UserProfile { Id = Guid.NewGuid(), Name = name, AdventureLevel = 1 };
            items.Add(created);
            selectedId = created.Id;
            After("create");
        }
        public void Rename(Guid id, string name)
        {
            RenameCalls++;
            Before("rename");
            if (NoopOperation == "rename") return;
            foreach (UserProfile item in items)
                if (item.Id != id && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("用户名已存在");
            Require(id).Name = name;
            After("rename");
        }
        public void Select(Guid id)
        {
            SelectCalls++;
            Before("select");
            if (NoopOperation == "select") return;
            Require(id);
            selectedId = id;
            After("select");
        }
        public void Delete(Guid id)
        {
            DeleteCalls++;
            Before("delete");
            if (NoopOperation == "delete") return;
            items.Remove(Require(id));
            if (selectedId == id) selectedId = items.Count == 0 ? (Guid?)null : items[0].Id;
            After("delete");
        }
        private UserProfile Require(Guid id)
        {
            UserProfile result = items.Find(item => item.Id == id);
            if (result == null) throw new KeyNotFoundException();
            return result;
        }
        private void Before(string operation)
        { if (FailOperation == operation) throw new IOException("写入失败"); }
        private void After(string operation)
        { if (CommitThenThrow == operation) throw new IOException("提交结果不确定"); }
    }
}
