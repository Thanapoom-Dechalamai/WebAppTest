using TodoListApp.Models;

namespace TodoListApp.Data
{
    /// <summary>
    /// Very simple thread-safe in-memory storage for TodoItem objects.
    /// Registered as a Singleton in Program.cs so the list survives
    /// across requests (but resets when the app restarts).
    /// Swap this out for an EF Core DbContext + real database later.
    /// </summary>
    public class TodoStore
    {
        private readonly List<TodoItem> _items = new();
        private readonly object _lock = new();
        private int _nextId = 1;

        public TodoStore()
        {
            // Seed with a couple of sample items
            Add(new TodoItem { Title = "ตั้งค่าโปรเจกต์ ASP.NET MVC", Description = "สร้างโครงสร้างโปรเจกต์เริ่มต้น", IsCompleted = true });
            Add(new TodoItem { Title = "เขียน CRUD สำหรับ Todo", Description = "Create, Read, Update, Delete", DueDate = DateTime.Today.AddDays(3) });
            Add(new TodoItem { Title = "ทดสอบระบบ", DueDate = DateTime.Today.AddDays(7) });
        }

        public IEnumerable<TodoItem> GetAll()
        {
            lock (_lock)
            {
                return _items.OrderBy(i => i.IsCompleted).ThenBy(i => i.DueDate).ToList();
            }
        }

        public TodoItem? GetById(int id)
        {
            lock (_lock)
            {
                return _items.FirstOrDefault(i => i.Id == id);
            }
        }

        public TodoItem Add(TodoItem item)
        {
            lock (_lock)
            {
                item.Id = _nextId++;
                _items.Add(item);
                return item;
            }
        }

        public bool Update(TodoItem item)
        {
            lock (_lock)
            {
                var existing = _items.FirstOrDefault(i => i.Id == item.Id);
                if (existing == null) return false;

                existing.Title = item.Title;
                existing.Description = item.Description;
                existing.IsCompleted = item.IsCompleted;
                existing.DueDate = item.DueDate;
                return true;
            }
        }

        public bool Delete(int id)
        {
            lock (_lock)
            {
                var existing = _items.FirstOrDefault(i => i.Id == id);
                if (existing == null) return false;
                return _items.Remove(existing);
            }
        }

        public bool ToggleComplete(int id)
        {
            lock (_lock)
            {
                var existing = _items.FirstOrDefault(i => i.Id == id);
                if (existing == null) return false;
                existing.IsCompleted = !existing.IsCompleted;
                return true;
            }
        }
    }
}
