using Microsoft.AspNetCore.Mvc;
using TodoListApp.Data;
using TodoListApp.Models;

namespace TodoListApp.Controllers
{
    public class TodoController : Controller
    {
        private readonly TodoStore _store;

        public TodoController(TodoStore store)
        {
            _store = store;
        }

        // GET: /Todo  or  /Todo/Index
        public IActionResult Index()
        {
            var items = _store.GetAll();
            return View(items);
        }

        // GET: /Todo/Create
        public IActionResult Create()
        {
            return View(new TodoItem());
        }

        // POST: /Todo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TodoItem item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _store.Add(item);
            TempData["Message"] = $"เพิ่มงาน \"{item.Title}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Todo/Edit/5
        public IActionResult Edit(int id)
        {
            var item = _store.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: /Todo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, TodoItem item)
        {
            if (id != item.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _store.Update(item);
            TempData["Message"] = $"แก้ไขงาน \"{item.Title}\" เรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Todo/Delete/5
        public IActionResult Delete(int id)
        {
            var item = _store.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: /Todo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _store.GetById(id);
            _store.Delete(id);
            TempData["Message"] = item != null ? $"ลบงาน \"{item.Title}\" เรียบร้อยแล้ว" : "ลบงานเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Todo/ToggleComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleComplete(int id)
        {
            _store.ToggleComplete(id);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
