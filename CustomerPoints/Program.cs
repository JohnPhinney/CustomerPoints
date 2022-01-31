using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerPoints
{
    class Program
    {
        static void Main(string[] args)
        {
            string connection = "";

            CustomerPointsSrvc svc = new CustomerPointsSrvc();
            svc.ProcessCustomerPoints(connection);
        }
    }
}
