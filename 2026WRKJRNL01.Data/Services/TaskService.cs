using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using WRKJRNL01.Data.Models;
using WRKJRNL01.Data.Repository;

namespace WRKJRNL01.Data.Services
{
    public class TaskService
    {
        protected TaskRepo TaskRepo { get; }
        protected CatRepo CatRepo { get; }

        public TaskService(TaskRepo taskRepo, CatRepo catRepo)
        {
            ArgumentNullException.ThrowIfNull(taskRepo);
            ArgumentNullException.ThrowIfNull(catRepo);

            TaskRepo = taskRepo;
            CatRepo = catRepo;
        }

        //this is the upsert task method
        public async Task<Tasque> CreateTask(Tasque task)
        {
            /* 
             look at the tasks date and ensure data is valid
             check if category exists
            upsert category if not exist?
            upsert task
            save
            return task
             */

            //check for emplId
            if (string.IsNullOrEmpty(task.EmplId))
            {
                //return error
                throw new Exception("EmplId was empty!");
            }

            //prevent date from future
            if (task.Date > DateTime.Now)
            {
                //return error
                throw new Exception("great scott! date is in the future!");
            }

            //check if the category exists?
            var catExists = await CatRepo.Get(task.CatId);

            if (catExists == null || catExists == default!)
            {
                //return error
                throw new Exception("category does not exist!");
            }
            
            //upsert task
            var result = await TaskRepo.Upsert(task);

            return result;
        }

        //get all cats entirely
        public Task<List<Tasque>> GetAllTasks(bool includeDeleted)
        {
            return TaskRepo.GetAllTasksAsync(includeDeleted);
        }

        //get all dept cats and customCats dept wide
        public Task<List<Category>> GetAllCategoriesAsync(string? deptId, bool includeCustom)
        {
            return CatRepo.GetAllCategoriesAsync(deptId, includeCustom);
        }

        //get all dept cats and user cats for a given user
        public Task<List<Category>> GetAllCategoriesAsync(string? deptId, string? empId)
        {
            return CatRepo.GetAllCategoriesAsync(deptId, empId);
        }
    }

}

