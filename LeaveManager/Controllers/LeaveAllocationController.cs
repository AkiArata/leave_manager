using AutoMapper;
using LeaveManager.Contracts;
using LeaveManager.Data;
using LeaveManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeaveAllocationController : Controller
    {
        //private readonly ILeaveTypeRepository _leaveRepo;
        //private readonly ILeaveAllocationRepository _leaveAllocationRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveAllocationController(
            //ILeaveTypeRepository leaverepo,
            //ILeaveAllocationRepository leaveallocationrepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            //_leaveRepo = leaverepo;
            //_leaveAllocationRepo = leaveallocationrepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        // GET: LeaveAllocation
        public async Task<ActionResult> Index()
        {
            var leavetypes =await _unitOfWork.LeaveTypes.FindAll();
            var mappedLeaveTypes = _mapper.Map<List<LeaveType>, List<LeaveTypeVM>>((List<LeaveType>)leavetypes);
            var model = new CreateLeaveAllocationVM
            {
                LeaveTypes = mappedLeaveTypes,
                NumberUpdated = 0
            };
            return View(model);
        }

        public async Task<ActionResult> SetLeave(int id)
        {
            var leavetype =await _unitOfWork.LeaveTypes.Find(x=>x.Id==id);
            var employees =await _userManager.GetUsersInRoleAsync("Employee");
            var period = DateTime.Now.Year;
            foreach (var emp in employees)
            {
                //if (await _leaveAllocationRepo.CheckAllocation(id, emp.Id))
                if (await _unitOfWork.LeaveAllocations.isExists(q => q.EmployeeId == emp.Id
                                       && q.LeaveTypeId == id
                                       && q.Period == period))
                    continue;
                var allocation = new LeaveAllocationVM
                {
                    DateCreated = DateTime.Now,
                    EmployeeId = emp.Id,
                    LeaveTypeId = id,
                    NumberOfDays = leavetype.DefaultDays,
                    Period = DateTime.Now.Year
                };
                var leaveallocation = _mapper.Map<LeaveAllocation>(allocation);
                // await _leaveAllocationRepo.Create(leaveallocation);
                await _unitOfWork.LeaveAllocations.Create(leaveallocation);
                await _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> ListEmployees()
        {
            var employees =await _userManager.GetUsersInRoleAsync("Employee");
            var model = _mapper.Map<List<EmployeeVM>>(employees);
            return View(model);
        }

        // GET: LeaveAllocation/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var employee = _mapper.Map<EmployeeVM>(await _userManager.FindByIdAsync(id));
            //var leaveAllocations = _leaveAllocationRepo.GetLeaveAllocationsByEmployee(id);
            var period = DateTime.Now.Year;
            //var allocations = _mapper.Map<List<LeaveAllocationVM>>(await _leaveallocationrepo.GetLeaveAllocationsByEmployee(id));
            var records = await _unitOfWork.LeaveAllocations.FindAll(
                expression: q => q.EmployeeId == id && q.Period == period,
                includes: q => q.Include(x => x.LeaveType)
            );

            var allocations = _mapper.Map<List<LeaveAllocationVM>> (records);
            var model = new ViewAllocationsVM
            {
                Employee = employee,
                LeaveAllocations = allocations
            };
            return View(model);
        }

        // GET: LeaveAllocation/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //var leaveallocation =await _leaveAllocationRepo.FindById(id);
            var leaveallocation = await _unitOfWork.LeaveAllocations
                                .Find(q => q.Id == id,
                                    includes: q => q.Include(x => x.Employee).Include(x => x.LeaveType));

            var model = _mapper.Map<EditLeaveAllocationVM>(leaveallocation);
            return View(model);
        }

        // POST: LeaveAllocation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditLeaveAllocationVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                // var record =await _leaveAllocationRepo.FindById(model.Id);
                var record = await _unitOfWork.LeaveAllocations.Find(q => q.Id == model.Id);

                record.NumberOfDays = model.NumberOfDays;
                //var isSuccess =await _leaveAllocationRepo.Update(record);
                //if (!isSuccess)
                //{
                //    ModelState.AddModelError("", "Error while saving");
                //    return View(model);
                //}
                _unitOfWork.LeaveAllocations.Update(record);
                await _unitOfWork.Save();
                return RedirectToAction(nameof(Details), new { id = model.EmployeeId });
            }
            catch
            {
                return View(model);
            }
        }
    }
}