using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;

namespace SocketService
{
    public abstract class DBHelper
    {

        public MySqlConnection CreateConn()
        {
            string _strconn = ConfigurationSettings.AppSettings["SqlConn"].ToString();
            MySqlConnection conn = new MySqlConnection(_strconn);
            return conn;
        }
    }

    #region MS数据库操作类
    /// <summary>
    /// MS数据库操作类
    /// </summary>
    public class SqlDBHelper : DBHelper
    {
        #region ADO.NET组件
        private MySqlConnection conn;
        private MySqlCommand cmd;
        private MySqlDataAdapter sda;
        private DataSet ds;
        #endregion

        #region 执行sql命令函数
        /// <summary>
        /// 执行sql命令函数
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns name="count">返回所影响的行数</returns>
        public int RunSqlCmd(string sql)
        {
            //SqlCmd所影响的行数
            int count = 0;
            try
            {
                conn = CreateConn();
                conn.Open(); //打开数据库连接
                cmd = new MySqlCommand(sql, conn);
                count = cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                //抛出异常
                throw;
            }
            finally
            {
                //执行成功后，关闭数据库连接
                conn.Close();
            }
            return count;
        }
        #endregion

        #region 创建SqlCommand来执行SQL语句
        /// <summary>
        /// 创建SqlCommand来执行SQL语句
        /// </summary>
        /// <param name="sqlStr"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public MySqlCommand CreateCmdSql(string sqlStr)
        {
            try
            {
                conn = CreateConn();
                cmd = new MySqlCommand(sqlStr, conn);
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            return cmd;
        }
        #endregion

        #region 创建SqlCommand来执行存储过程(带参数)
        /// <summary>
        /// 创建SqlCommand来执行存储过程(带参数)
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public MySqlCommand CreateCmd(string procName, MySqlParameter[] para)
        {
            try
            {
                conn = CreateConn(); //得到数据库连接
                cmd = new MySqlCommand(); //设置Command对象
                cmd.CommandType = CommandType.Text; //创建存储过程
                cmd.CommandText = procName; //调用存储过程名称
                cmd.Connection = conn; //创建数据库连接对象
                if (para != null)
                {
                    //添加存储过程参数
                    foreach (MySqlParameter sp in para)
                    {
                        cmd.Parameters.Add(sp);
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            return cmd;
        }
        #endregion

        #region 创建SqlCommand来执行存储过程(不带参数)
        /// <summary>
        /// 创建SqlCommand来执行存储过程(不带参数)
        /// </summary>
        /// <param name="procName"></param>
        /// <returns></returns>
        public MySqlCommand CreateCmd(string procName)
        {
            try
            {
                conn = CreateConn(); //得到数据库连接
                cmd = new MySqlCommand(); //设置Command对象
                cmd.CommandType = CommandType.StoredProcedure; //创建存储过程
                cmd.CommandText = procName; //调用存储过程名称
                cmd.Connection = conn; //创建数据库连接对象
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            return cmd;
        }
        #endregion

        #region 对表进行增删改操作
        /// <summary>
        /// 对表进行增删改操作
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        /// <param name="para">参数</param>
        /// <returns></returns>
        public int RunSql(string procName, MySqlParameter[] para)
        {
            int count = 0;
            try
            {
                //得到SqlCommand对象
                cmd = CreateCmd(procName, para);
                conn.Open();
                count = cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
            return count;
        }
        #endregion

        #region 查询DataSet(带参数)
        /// <summary>
        /// 查询DataSet(带参数)
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        /// <param name="para">参数</param>
        /// <returns></returns>
        public DataSet GetDataSet(string procName, MySqlParameter[] para)
        {
            try
            {
                //得到SqlCommand对象
                cmd = CreateCmd(procName, para);
                sda = new MySqlDataAdapter();
                sda.SelectCommand = cmd;
                ds = new DataSet();
                sda.Fill(ds);
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
            return ds;
        }
        #endregion

        #region 查询DataSet(不带参数)
        /// <summary>
        /// 查询DataSet(不带参数)
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        /// <returns></returns>
        public DataSet GetDataSet(string procName)
        {
            try
            {
                //得到SqlCommand对象
                cmd = CreateCmd(procName);
                sda = new MySqlDataAdapter();     //a语句
                sda.SelectCommand = cmd;        //b语句——a和b语句等同于SqlDataAdapter sda = new SqlDataAdapter(cmd)
                ds = new DataSet();
                sda.Fill(ds);
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
            return ds;
        }
        #endregion

        #region 依据Sql语句查询DataSet
        ///<summary>
        ///依据Sql语句查询DataSet
        ///</summary>
        #endregion
        public DataSet GetDataSetBySql(string sqlstr)
        {
            try
            {
                //得到SqlCommand对象
                cmd = CreateCmdSql(sqlstr);
                MySqlDataAdapter sda = new MySqlDataAdapter(cmd);
                ds = new DataSet();
                sda.Fill(ds);
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
            return ds;
        }
    }
    #endregion
}
