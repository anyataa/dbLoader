using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerService1
{
    //a : varchar(100)
    //b : varchar(100)
    //c : varchar(100) 
    //d : int
    //e : int 
    //f : bigInt 

    class Config
    {
        public string iterateCustomer() {
            Dictionary<string, string> customer = new Dictionary<string, string>
            {
                { "a" , "f_name"},
                {"b", "l_name" }
            };

            foreach (KeyValuePair<string, string> entry in customer) {
                return (entry.Key + " " + entry.Value);
            }
            return ("anya");
        }

    }
}
