//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebAccess.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class store
    {
        public store()
        {
            this.AspNetUsers = new HashSet<AspNetUser>();
        }
    
        public int id { get; set; }
        public string storename { get; set; }
        public Nullable<int> WebUserId { get; set; }
        public string storecode { get; set; }
        public string connectionstring { get; set; }
        public string hmac { get; set; }
        public string salt { get; set; }
        public string dbserver { get; set; }
        public string dbname { get; set; }
        public string dbpassword { get; set; }
    
        public virtual ICollection<AspNetUser> AspNetUsers { get; set; }
    }
}