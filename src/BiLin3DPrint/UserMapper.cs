using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bilin3d.Models;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.Data;


namespace Bilin3d {
    public class UserMapper : IUserMapper {
        //private IDocumentSession DocumentSession;

        private IDbConnection DB;

        public UserMapper(IDbConnection db) {
            this.DB = db;
        }              

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context) {
           
            var userRecord = DB.Select<UserModel>(q => q.UserGuid == identifier).FirstOrDefault();

            return userRecord == null ? null : new UserIdentity() { UserName = userRecord.Email };
        }

        public Guid? ValidateUser(string email, string password) {
            //var userRecord = DB.Select<UserModel>(q => q.Email == email && q.PassWord == EncodePassword(password)).FirstOrDefault();
            //var userRecord = DB.Select<UserModel>(q => q.Email == email && q.PassWord == password).FirstOrDefault();
            var userRecord = DB.Single<UserModel>(q => q.Email == email && q.PassWord == password);
            if (userRecord == null) return null;

            return userRecord.UserGuid;
        }

        public Guid? ValidateRegisterNewUser(RegisterModel newUser) {
            var userRecord = new UserModel() {
                UserGuid = Guid.NewGuid(),
                Email = newUser.Email,
                //NickName = newUser.NickName,
                PassWord = newUser.Password,
                //PassWord = EncodePassword(newUser.Password)
            };

            IDbDataParameter email = DB.CreateParam("email", userRecord.Email, dbType: DbType.String);
            var existingUser = DB.Select<UserModel>("select * from T_User where Email=@email", new[]{email} ).FirstOrDefault();
            //var existingUser = DB.Select<UserModel>(q => q.Email == userRecord.Email).FirstOrDefault();
            if (existingUser != null)
                return null;

            DB.Insert(userRecord);            

            return userRecord.UserGuid;
        }

        private string EncodePassword(string originalPassword) {
            if (originalPassword == null)
                return String.Empty;

            //Declarations
            Byte[] originalBytes;
            Byte[] encodedBytes;
            MD5 md5;

            //Instantiate MD5CryptoServiceProvider, get bytes for original password and compute hash (encoded password)
            md5 = new MD5CryptoServiceProvider();
            originalBytes = ASCIIEncoding.Default.GetBytes(originalPassword);
            encodedBytes = md5.ComputeHash(originalBytes);

            //Convert encoded bytes back to a 'readable' string
            return BitConverter.ToString(encodedBytes);
        }

    }
}