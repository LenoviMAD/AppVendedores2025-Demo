using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppVendedores2025.Services
{
    public class FailedEmailService
    {
        private SQLiteAsyncConnection _dbConnection;

        public FailedEmailService()
        {
            SetUpDb();
        }

        private void SetUpDb()
        {
            if (_dbConnection == null)
            {
                _dbConnection =  DABSqlLite.SetUpDb();
                _dbConnection.CreateTableAsync<FailedEmail>().GetAwaiter().GetResult();
            }
        }

        public async Task<int> AddFailedEmail(FailedEmail email)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }
            return await _dbConnection.InsertAsync(email);
        }

        public async Task<int> SaveFailedEmailAsync(string email, string userName, string subject, string body, string status)
        {
            var failedEmail = new FailedEmail
            {
                Email = email,
                UserName = userName,
                Subject = subject,
                Body = body,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            return await AddFailedEmail(failedEmail);
        }

        public async Task<List<FailedEmail>> GetAllFailedEmails()
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            return await _dbConnection.Table<FailedEmail>().ToListAsync();
        }

        public async Task<int> DeleteFailedEmail(int id)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            var email = await GetFailedEmailById(id);
            return email != null ? await _dbConnection.DeleteAsync(email) : 0;
        }

        public async Task<FailedEmail> GetFailedEmailById(int id)
        {
            if (_dbConnection == null)
            {
                SetUpDb();
            }

            return await _dbConnection.FindAsync<FailedEmail>(id);
        }

        // Otros métodos según necesites
    }
}
