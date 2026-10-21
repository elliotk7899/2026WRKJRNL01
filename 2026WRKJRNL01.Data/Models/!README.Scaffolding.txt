For more information on EF Scaffolding, see here:  https://docs.microsoft.com/en-us/ef/core/cli/powershell 

1 - In the VS Developer PowerShell, change into the folder for your Data project.
2 - In the PowerShell tab, paste the following.  Be sure to replace the marked values with those for your project:

dotnet ef dbcontext scaffold "Data Source={INSERT DATABASE SERVER HERE};Initial Catalog={INSERT DATABASE NAME HERE};Integrated Security=True;MultipleActiveResultSets=True;App=EntityFramework" Microsoft.EntityFrameworkCore.SqlServer --context-dir Models --namespace {INSERT DATA PROJECT NAMESPACE HERE} --context-namespace {INSERT DATA PROJECT NAMESPACE HERE}

3 - Once scaffolding is complete, organize the newly created items as you see fit.  
4 - To enable compatability with projects that deal directly with connection strings (standalone windows services, etc.)
    please modify your newly created context to inherit from the DbContextExtended class provided by the SDBPC.Data.Common namespace.

    Of note, the following needs to be added:

    1 - A constructor which accepts and passes a connection string to the DbContextExtended base class.

        public MyContext(string connectionString) : base(connectionString)
        { }

    2 - An override for the OnConfiguring event which passes a true / false value for the optional disableTracking parameter. 
        (https://learn.microsoft.com/en-us/ef/core/change-tracking/)

        If enabled (default), change tracking in EF is disabled which both increases performance and  removes the potential for 
        inadvertant updates to data objects if used in multiple areas of the project.

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
          base.OnConfiguring(optionsBuilder, true);
        }

Please note, objects created through EF Scaffolding should be treated as “auto-generated” code.  
To avoid headaches in future if scaffolding ever needs to be re-run its wise to modify these as little as possible.

If re-running the EF scaffolder, its best to back up and clear out the data project beforehand to avoid any unexpected compilation errors.

1 - Cut / paste the Models and Repository folders into a safe location outside of the project folder.
2 - Re-run the steps as described above.
3 - When finished, copy your reposistory folder back into the project.
