using LeaveManager.Contracts;
using LeaveManager.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveManager.Repositories
{
    public class LeaveTypeRepository : ILeaveTypeRepository
    {
        private readonly ApplicationDbContext _db;
        public LeaveTypeRepository(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<bool> Create(LeaveType entity)
        {
            await _db.LeaveTypes.AddAsync(entity);
            return await Save();
        }

        public async Task<bool> Delete(LeaveType entity)
        {
            _db.LeaveTypes.Remove(entity);
            return await Save();
        }

        public async Task<ICollection<LeaveType>> FindAll()
        {
            var result=await _db.LeaveTypes.ToListAsync();
            return result;
        }

        public async Task<LeaveType> FindById(int id)
        {
            var result= await _db.LeaveTypes.FindAsync(id);
            return result;
        }

        public Task<ICollection<LeaveType>> GetEmployeesByLeaveType(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Save()
        {
            var changes=await _db.SaveChangesAsync();
            return changes > 0;
        }
        public async Task<bool> isExists(int id)
        {
            var exists =await _db.LeaveTypes.AnyAsync(q => q.Id == id);
            return exists;
        }
        public async Task<bool> Update(LeaveType entity)
        {
            _db.LeaveTypes.Update(entity);
            return await Save();
        }
    }
}
