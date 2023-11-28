using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [DataContract]
    public class BankAccount
    {
        [DataMember]
        public long AccountNumber { get; set; }
        [DataMember]
        public double AmountOfMoney { get; set; }

    }
}
