using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerPoints
{
    public class CustomerPointsSrvc
    {
        public bool ProcessCustomerPoints(string conn)
        {
            bool blRet = false;

            try
            {
                InitializeCustomerPoints(conn);
                CalculateCustPoints(conn);
            }
            catch (Exception ex)
            {
                blRet = false;
            }

            return (blRet);
        }

        private bool InitializeCustomerPoints(string conn)
        {
            bool blRet = false;
            System.Data.DataSet ds = new DataSet1();

            try
            {
                SqlConnection con = new SqlConnection(conn);
                SqlDataAdapter txns = new SqlDataAdapter("SELECT * FROM Transaction", con);
                txns.UpdateCommand = new SqlCommand("update Transaction SET Month = @Month, TxnPoints = @TxnPoints where TxnId = @TxnId");

                SqlDataAdapter cust = new SqlDataAdapter("SELECT * FROM Customer", con);
                SqlDataAdapter custmonth = new SqlDataAdapter("SELECT * FROM CustMonthlyPoints", con);

                txns.Fill(ds, "Transaction");
                //      cust.Fill(ds, "Customer");
                //      custmonth.Fill(ds, "CustMonthlyPoints");

                int Points = 0;
                decimal Amount = 0;
                DateTime dt;
                SqlParameter prm1 = txns.UpdateCommand.Parameters.Add("@Month", SqlDbType.VarChar, 20, "Month");
                SqlParameter prm2 = txns.UpdateCommand.Parameters.Add("@TxnId", SqlDbType.VarChar, 20, "TxnId");
                SqlParameter prm3 = txns.UpdateCommand.Parameters.Add("@TxnPoints", SqlDbType.Int, 0, "TxnPoints");

                foreach (DataRow row in ds.Tables["Transaction"].Rows)
                {
                    dt = (DateTime)row["Date"];
                    Amount = (decimal)row["Amount"];
                    row["Month"] = dt.Month.ToString();

                    if (Amount <= 50)
                        Points = 0;
                    else if (Amount <= 100)
                    {
                        Points = (int)Amount - 50;
                    }
                    else
                    {
                        Points = ((int)Amount - 100) * 2;
                        Points = Points + 50;
                    }

                    string s = dt.Month.ToString();
                    prm1.Value = s;
                    prm1.Size = s.Length;

                    s = (string)row["TxnId"].ToString();
                    prm2.Value = s;
                    prm2.Size = s.Length;

                    prm3.Value = (int)Points;
                    txns.Update(ds.Tables["Transaction"]);
                }

                blRet = true;
            }
            catch (Exception ex)
            {
                blRet = false;
            }

            return (blRet);
        }

        private bool CalculateCustPoints(string conn)
        {
            bool blRet = false;
            SqlConnection con = new SqlConnection(conn);
            SqlCommand cmd = new SqlCommand("select * from Transaction order by CustomerId, Month", con);
            SqlDataReader reader = cmd.ExecuteReader();
            string cust = "";
            string month = "";
            string custnew = "";
            string monthnew = "";
            int iTotalMonthPoints = 0;
            int iTotalCustPoints = 0;

            try
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        if (cust == "")
                            cust = reader["CustomerId"].ToString();
                        else
                        {
                            custnew = reader["CustomerId"].ToString();

                            if (custnew != cust)
                            {
                                // Write CustomerMonth 
                                UpsertCustomerMonthly(conn, cust, month, iTotalMonthPoints);

                                // Write Customer
                                UpsertCustomer(conn, cust, iTotalCustPoints);
                                iTotalCustPoints = 0;
                                iTotalMonthPoints = 0;
                                cust = custnew;
                                month = "";
                            }
                        }

                        if (month == "")
                            month = reader["Month"].ToString();
                        else
                        {
                            monthnew = reader["Month"].ToString();

                            if (monthnew != month)
                            {
                                // Write CustomerMonth
                                UpsertCustomerMonthly(conn, cust, month, iTotalMonthPoints);

                                iTotalMonthPoints = 0;
                                month = monthnew;
                            }
                        }

                        int iPoints = 0;
                        iPoints = (int)reader["TxnPoints"];
                        iTotalMonthPoints += iPoints;
                        iTotalCustPoints += iPoints;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return blRet;
        }

        private void UpsertCustomerMonthly(string conn, string cust, string month, int iTotalMonthPoints)
        {
            SqlConnection con = new SqlConnection(conn);
            SqlCommand cmd = new SqlCommand("select * from CustMonthlyPoints where CustomerId = '" + cust + "' and Month = '" + month + "'", con);
            SqlDataReader reader = cmd.ExecuteReader();

            try
            {
                if (reader.HasRows == false)
                    cmd.CommandText = "insert CustMonthlyPoints (CustomerId, Month, TotalMonthlyPoints) values ('" + cust + "', '" + month + "', " + iTotalMonthPoints.ToString() + ")";
                else
                    cmd.CommandText = "Update CustMonthlyPoints set TotalMonthlyPoints = " + iTotalMonthPoints.ToString() + " where CustomerId = '" + cust + "' and Month = '" + month + "'";

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
            }
        }

        private void UpsertCustomer(string conn, string cust, int iTotalCustPoints)
        {
            SqlConnection con = new SqlConnection(conn);
            SqlCommand cmd = new SqlCommand("select * from Customer where CustomerId = '" + cust + "'", con);
            SqlDataReader reader = cmd.ExecuteReader();
            int iPoints = 0;

            try
            {
                if (reader.HasRows == false)
                {
                    cmd.CommandText = "insert Customer (CustomerId, TotalPoints) values ('" + cust + "', " + iTotalCustPoints.ToString() + ")";
                }
                else
                {
                    reader.Read();
                    iPoints = (int)reader["TotalPoints"];
                    iPoints += iTotalCustPoints;

                    cmd.CommandText = "Update Customer set TotalPoints = " + iPoints.ToString() + "where CustomerId = '" + cust + "'";
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
