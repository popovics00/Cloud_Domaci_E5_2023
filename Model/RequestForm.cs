using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Model
{
    [DataContract]
    public class RequestForm
    {
        [DataMember]
        [Display(Name = "FirstName")]
        public string FirstName { get; set; }

        [DataMember]
        [Display(Name = "LastName")]
        public string LastName { get; set; }

        [DataMember]
        public int BookId { get; set; }
        
        [DataMember]
        public int BookCount { get; set; }
        
        [DataMember]
        public int AccountNumber { get; set; }
    }
}
