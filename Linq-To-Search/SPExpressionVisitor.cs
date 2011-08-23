using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Globalization;

namespace LinqTest
{

    internal sealed class SPExpressionVisitor : ExpressionVisitor
    {
        private StringBuilder _urlBuilder;
        private Dictionary<string, string> _queryString;


        // query string. Needs to be far more complex than this.
        private string query = string.Empty;

        // list of query terms to search for.
        private List<string> queryStrings = new List<string>();

		private bool result = false;
		
        public SPExpressionVisitor()
        {

        }




		
		// what should it return????
        public string ProcessExpression(Expression expression)
        {
            // clear querystrings
            queryStrings.Clear();

            // parse expression tree
            Visit((MethodCallExpression)expression);

            var result = GenerateQueryRequest();

            return result;
        }

        private string GenerateQueryRequest()
        {
            // concat all elements of the queryString list.
            var q = "";
            foreach( var i in queryStrings)
            {
                q += i + " ";
            }

            //The XML string containing the query request information
            string qXMLString = "<QueryPacket xmlns='urn:Microsoft.Search.Query'>" +
                  "<Query><SupportedFormats><Format revision='1'>" +
                  "urn:Microsoft.Search.Response.Document:Document</Format>" +
                  "</SupportedFormats>" +
                  "<Context>" +
                  "<QueryText language='en-US' type='STRING'>" +
                  q +
                  "</QueryText></Context>" +
                  "</Query></QueryPacket>";

            Console.WriteLine("Query is " + qXMLString);
            return qXMLString;

        }

        // override ExpressionVisitor method
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if ((m.Method.DeclaringType == typeof(Queryable)) || (m.Method.DeclaringType == typeof(Enumerable)))
            {
                if (m.Method.Name.Equals("Where"))
                {
                    LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    query = ParseQuery(lambda.Body);

                    return m;
                }

            }

            return m;
        }
		
		
        internal string ParseQuery(Expression e)
        {
            string q = null;

            if (e is BinaryExpression)
            {
                BinaryExpression c = e as BinaryExpression;
					
                switch (c.NodeType)
                {
                    case ExpressionType.Equal:
                        GetCondition(c);
                        
                        break;

                    case ExpressionType.AndAlso:

                        // go through parse each branch of the AND statement.
                        AndAlsoCondition(c);
                        break;


                    default:
                        throw new NotSupportedException("Only .Equal is supported for this query");
                }
            }
			
			
            else
                throw new NotSupportedException("This querytype is not supported.");

            return q;
        }


        // just try and do both branches of the tree?
        // 
        internal void AndAlsoCondition(BinaryExpression e)
        {
            ParseQuery(e.Left);
            ParseQuery(e.Right);
           
        }

        internal void GetCondition(BinaryExpression e)
        {
            string val = string.Empty, attrib = string.Empty;
            string q = null;
		
            if (e.Left is MemberExpression)
            {
                attrib = ((MemberExpression)e.Left).Member.Name;
                val = Expression.Lambda(e.Right).Compile().DynamicInvoke().ToString();
            }
            else if (e.Right is MemberExpression)
            {
                attrib = ((MemberExpression)e.Right).Member.Name;
                val = Expression.Lambda(e.Left).Compile().DynamicInvoke().ToString();
            }

            // add all?
            queryStrings.Add(val);

            /* 
            if (attrib.Equals("Query"))
            {
                if (query.Length > 0)
                    throw new NotSupportedException("'WikipediaOpenSearchResult' Query expression can only contain one 'Keyword'");
                else
                {
                    queryStrings.Add(val);

                }
            }
            else
            {
                throw new NotSupportedException("'WikipediaOpenSearchResult' Query expression can only contain a'Keyword' parameter");
            }
             * 
             * */

        }
    }
}