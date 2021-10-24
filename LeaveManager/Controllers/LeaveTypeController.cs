using AutoMapper;
using LeaveManager.Contracts;
using LeaveManager.Data;
using LeaveManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveManager.Controllers
{
    [Authorize(Roles ="Admin")]
    public class LeaveTypeController : Controller
    {
        //private readonly ILeaveTypeRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public LeaveTypeController(IUnitOfWork unitOfWork,
            IMapper mapper
            /*ILeaveTypeRepository repo,*/)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: LeaveTypeController
        public async Task<ActionResult> Index()
        {
            var leaveTypes =await _unitOfWork.LeaveTypes.FindAll();
            var model = _mapper.Map<List<LeaveType>, List<LeaveTypeVM>>((List<LeaveType>)leaveTypes);
            return View(model);
        }

        // GET: LeaveTypes/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var result = await _unitOfWork.LeaveTypes.isExists(x=>x.Id==id);
            if (!result)
            {
                return NotFound();
            }
            var leavetype =await _unitOfWork.LeaveTypes.Find(x=>x.Id==id);
            var model = _mapper.Map<LeaveTypeVM>(leavetype);
            return View(model);
        }

        // GET: LeaveTypes/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LeaveTypeVM model)
        {
            try
            {
                // TODO: Add insert logic here
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var leaveType = _mapper.Map<LeaveType>(model);
                leaveType.DateCreated = DateTime.Now;

                await _unitOfWork.LeaveTypes.Create(leaveType);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Something Went Wrong...");
                return View(model);
            }
        }

        // GET: LeaveTypes/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var result = await _unitOfWork.LeaveTypes.isExists(x=>x.Id==id);
            if (!result)
            {
                return NotFound();
            }
            var leavetype = _unitOfWork.LeaveTypes.Find(x => x.Id == id);
            var model = _mapper.Map<LeaveTypeVM>(leavetype);
            return View(model);
        }

        // POST: LeaveTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LeaveTypeVM model)
        {
            try
            {
                // TODO: Add update logic here
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var leaveType = _mapper.Map<LeaveType>(model);
                _unitOfWork.LeaveTypes.Update(leaveType);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Something Went Wrong...");
                return View(model);
            }
        }

        // GET: LeaveTypes/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var leavetype =await _unitOfWork.LeaveTypes.Find(x => x.Id == id);
            if (leavetype == null)
            {
                return NotFound();
            }
            _unitOfWork.LeaveTypes.Delete(leavetype);
            await _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        // POST: LeaveTypes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, LeaveTypeVM model)
        {
            try
            {
                // TODO: Add delete logic here
                var leavetype =await _unitOfWork.LeaveTypes.Find(x => x.Id == id);
                if (leavetype == null)
                {
                    return NotFound();
                }
                _unitOfWork.LeaveTypes.Delete(leavetype);
                await _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(model);
            }
        }
    }
}
