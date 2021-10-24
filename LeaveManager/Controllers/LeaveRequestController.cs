using AutoMapper;
using LeaveManager.Contracts;
using LeaveManager.Data;
using LeaveManager.Models;
using LeaveManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveManager.Controllers
{
    public class LeaveRequestController : Controller
    {
        //private readonly ILeaveRequestRepository _leaveRequestRepo;
        //private readonly ILeaveTypeRepository _leaveTypeRepo;
        //private readonly ILeaveAllocationRepository _leaveAllocRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;
        private readonly IEmailSender _emailSender;
        public LeaveRequestController(
            //ILeaveRequestRepository leaveRequestRepo,
            //ILeaveTypeRepository leaveTypeRepo,
            //ILeaveAllocationRepository leaveAllocRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<Employee> userManager,
            IEmailSender emailSender
        )
        {
            //_leaveRequestRepo = leaveRequestRepo;
            //_leaveTypeRepo = leaveTypeRepo;
            //_leaveAllocRepo = leaveAllocRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [Authorize(Roles = "Admin")]
        // GET: LeaveRequest
        public async Task<ActionResult> Index()
        {
            //var leaveRequests =await _leaveRequestRepo.FindAll();
            var leaveRequests = await _unitOfWork.LeaveRequests.FindAll(
                  includes: q => q.Include(x => x.RequestingEmployee).Include(x => x.LeaveType)
              );

            var leaveRequstsModel = _mapper.Map<List<LeaveRequestVM>>(leaveRequests);
            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequstsModel.Count,
                ApprovedRequests = leaveRequstsModel.Count(q => q.Approved == true),
                PendingRequests = leaveRequstsModel.Count(q => q.Approved == null),
                RejectedRequests = leaveRequstsModel.Count(q => q.Approved == false),
                LeaveRequests = leaveRequstsModel
            };
            return View(model);
        }

        public async Task<ActionResult> MyLeave()
        {
            var employee = await _userManager.GetUserAsync(User);
            var employeeid = employee.Id;
            //var employeeAllocations =await _leaveAllocRepo.GetLeaveAllocationsByEmployee(employeeid);
            //var employeeRequests =await _leaveRequestRepo.GetLeaveRequestsByEmployee(employeeid);
            var employeeAllocations = await _unitOfWork.LeaveAllocations.FindAll(q => q.EmployeeId == employeeid,
                                                      includes: q => q.Include(x => x.LeaveType));

            var employeeRequests = await _unitOfWork.LeaveRequests.FindAll(q => q.RequestingEmployeeId == employeeid);

            var employeeAllocationsModel = _mapper.Map<List<LeaveAllocationVM>>(employeeAllocations);
            var employeeRequestsModel = _mapper.Map<List<LeaveRequestVM>>(employeeRequests);

            var model = new EmployeeLeaveRequestViewVM
            {
                LeaveAllocations = employeeAllocationsModel,
                LeaveRequests = employeeRequestsModel
            };

            return View(model);

        }

        // GET: LeaveRequest/Details/5
        public async Task<ActionResult> Details(int id)
        {
            //var leaveRequest =await _leaveRequestRepo.FindById(id);
            var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id,
                includes: q => q.Include(x => x.RequestingEmployee).Include(x => x.LeaveType).Include(x => x.ApprovedBy));
            var model = _mapper.Map<LeaveRequestVM>(leaveRequest);
            return View(model);
        }

        public async Task<ActionResult> ApproveRequest(int id)
        {
            try
            {
                var user =await _userManager.GetUserAsync(User);
                // var leaveRequest = await _leaveRequestRepo.FindById(id);
                var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id);

                var employeeid = leaveRequest.RequestingEmployeeId;
                var leaveTypeId = leaveRequest.LeaveTypeId;

                // var allocation = await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leaveTypeId);
                var period = DateTime.Now.Year;
                var allocation = await _unitOfWork.LeaveAllocations.Find(q => q.EmployeeId == employeeid
                                                    && q.Period == period
                                                    && q.LeaveTypeId == leaveTypeId);

                int daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;
                
                allocation.NumberOfDays = allocation.NumberOfDays - daysRequested;

                leaveRequest.Approved = true;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //await _leaveRequestRepo.Update(leaveRequest);
                //await _leaveAllocRepo.Update(allocation);
                _unitOfWork.LeaveRequests.Update(leaveRequest);
                _unitOfWork.LeaveAllocations.Update(allocation);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));

            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<ActionResult> RejectRequest(int id)
        {
            try
            {
                var user =await _userManager.GetUserAsync(User);
                //var leaveRequest =await _leaveRequestRepo.FindById(id);
                var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id);

                leaveRequest.Approved = false;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //await _leaveRequestRepo.Update(leaveRequest);
                _unitOfWork.LeaveRequests.Update(leaveRequest);
                await _unitOfWork.Save();
                return RedirectToAction(nameof(Index));

            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: LeaveRequest/Create
        public async Task<ActionResult> Create()
        {
            //var leaveTypes =await _leaveTypeRepo.FindAll();
            var leaveTypes = await _unitOfWork.LeaveTypes.FindAll();
            var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
            {
                Text = q.Name,
                Value = q.Id.ToString()
            });
            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypeItems
            };
            return View(model);
        }

        // POST: LeaveRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLeaveRequestVM model)
        {
            try
            {
                var startDate = DateTime.ParseExact(model.StartDate, "MM/dd/yyyy", null);
                var endDate = DateTime.ParseExact(model.EndDate, "MM/dd/yyyy", null);
                // var leaveTypes =await _leaveTypeRepo.FindAll();
                var leaveTypes = await _unitOfWork.LeaveTypes.FindAll();

                var employee =await _userManager.GetUserAsync(User);
                //var allocation =await _leaveAllocRepo.GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId);
                var period = DateTime.Now.Year;
                var allocation = await _unitOfWork.LeaveAllocations.Find(q => q.EmployeeId == employee.Id
                                                    && q.Period == period
                                                    && q.LeaveTypeId == model.LeaveTypeId);
               
                int daysRequested = (int)(endDate - startDate).TotalDays;
                var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()
                });
                model.LeaveTypes = leaveTypeItems;


                if (allocation == null)
                {
                    ModelState.AddModelError("", "You Have No Days Left");
                }
                if (DateTime.Compare(startDate, endDate) > 1)
                {
                    ModelState.AddModelError("", "Start Date cannot be further in the future than the End Date");
                }
                if (daysRequested > allocation.NumberOfDays)
                {
                    ModelState.AddModelError("", "You Do Not Sufficient Days For This Request");
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestVM
                {
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    LeaveTypeId = model.LeaveTypeId,
                    RequestComments = model.RequestComments
                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                //var isSuccess =await _leaveRequestRepo.Create(leaveRequest);

                //if (!isSuccess)
                //{
                //    ModelState.AddModelError("", "Something went wrong with submitting your record");
                //    return View(model);
                //}

                _unitOfWork.LeaveRequests.Update(leaveRequest);
                await _unitOfWork.Save();

                await _emailSender.SendEmailAsync("admin@gmail.com", "New Leave Request",
                 $"Please review this leave request. <a href='UrlOfApp/{leaveRequest.Id}'>Click Here</a>");
                return RedirectToAction("MyLeave");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(model);
            }
        }

        public async Task<ActionResult> CancelRequest(int id)
        {
            //var leaveRequest =await _leaveRequestRepo.FindById(id);
            var leaveRequest = await _unitOfWork.LeaveRequests.Find(q => q.Id == id);

            leaveRequest.Cancelled = true;
            //await _leaveRequestRepo.Update(leaveRequest);
            return RedirectToAction("MyLeave");
        }

    }
}

