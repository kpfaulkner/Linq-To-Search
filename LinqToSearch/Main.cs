using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System;
using System.Reflection;
using System.Dynamic;

namespace LinqToSearch
{
	
	public class Query<T> : IQueryable<T>
	{
	
	    QueryProvider provider;
	
	    Expression expression;
	
	
	
	    public Query(QueryProvider provider)
	    {
	
	        if (provider == null)
	        {
	
	            throw new ArgumentNullException("provider");
	
	        }
	
	        this.provider = provider;
	
	        this.expression = Expression.Constant(this);
	
	    }
	
	
	
	    public Query(QueryProvider provider, Expression expression)
	    {
	
	        if (provider == null)
	        {
	
	            throw new ArgumentNullException("provider");
	
	        }
	
	        if (expression == null)
	        {
	
	            throw new ArgumentNullException("expression");
	
	        }
	
	        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
	        {
	
	            throw new ArgumentOutOfRangeException("expression");
	
	        }
	
	        this.provider = provider;
	
	        this.expression = expression;
	
	    }
	
	
	
	    Expression IQueryable.Expression
	    {
	
	        get { return this.expression; }
	
	    }
	
	
	
	    Type IQueryable.ElementType
	    {
	
	        get { return typeof(T); }
	
	    }
	
	
	
	    IQueryProvider IQueryable.Provider
	    {
	
	        get { return this.provider; }
	
	    }
	
	
	
	    public IEnumerator<T> GetEnumerator()
	    {
	
			var res1 = this.provider.Execute(this.expression);
			var res2 = ((IEnumerable<T>) res1);
			
	        return res2.GetEnumerator();
	
	    }
	
	
	
	    IEnumerator IEnumerable.GetEnumerator()
	    {
	
	        return ((IEnumerable)this.provider.Execute(this.expression)).GetEnumerator();
	
	    }
	
	
	
	    public override string ToString()
	    {
	
	        return this.provider.GetQueryText(this.expression);
	
	    }
	
	}
	
	
	public abstract class QueryProvider : IQueryProvider
	{
	
	    protected QueryProvider()
	    {
	
	    }
	
	
	
	    IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
	    {
	
			Type elementType = TypeSystem.GetElementType(expression.Type);
			
			MethodCallExpression call = expression as MethodCallExpression;
            if (call != null)
            {
                //
                // First parameter to the method call represents the (unary) lambda in LINQ style.
                // E.g. (user => user.Name == "Bart") for a Where  clause
                //      (user => new { user.Name })   for a Select clause
                //
				
				Console.WriteLine("nt " + expression.NodeType.ToString() );
				Console.WriteLine("method " + call.Method.Name);
				Console.WriteLine("element " + typeof( S ).Name );
				foreach( var arg in call.Arguments)
				{
					if ( arg is ConstantExpression)
					{
						Console.WriteLine( "CONST " + (arg as ConstantExpression).Value );	
					}
					else
					{
						Console.WriteLine( "NON CONST " + arg );						
					}
				}
				
            }
			
	        return new Query<S>(this, expression);
	
	    }
	
	
	
	    IQueryable IQueryProvider.CreateQuery(Expression expression)
	    {
	
	        Type elementType = TypeSystem.GetElementType(expression.Type);
	
	        try
	        {
	
	            return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
	
	        }
	
	        catch (TargetInvocationException tie)
	        {
	
	            throw tie.InnerException;
	
	        }
	
	    }
	
	
	
	    S IQueryProvider.Execute<S>(Expression expression)
	    {
	
	        return (S)this.Execute(expression);
	
	    }
	
	
	
	    object IQueryProvider.Execute(Expression expression)
	    {
	
	        return this.Execute(expression);
	
	    }
	
	
	
	    public abstract string GetQueryText(Expression expression);
	
	    public abstract object Execute(Expression expression);
	
	}
	
		
	internal static class TypeSystem
	{
	
		internal static Type GetElementType(Type seqType)
		{
		
		    Type ienum = FindIEnumerable(seqType);
		
		    if (ienum == null) return seqType;
		
		    return ienum.GetGenericArguments()[0];
		
		}
		
		private static Type FindIEnumerable(Type seqType)
		{
		
		    if (seqType == null || seqType == typeof(string))
		
		        return null;
		
		    if (seqType.IsArray)
		
		        return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
		
		    if (seqType.IsGenericType)
		    {
		
		        foreach (Type arg in seqType.GetGenericArguments())
		        {
		
		            Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
		
		            if (ienum.IsAssignableFrom(seqType))
		            {
		
		                return ienum;
		
		            }
		
		        }
		
		    }
		
		    Type[] ifaces = seqType.GetInterfaces();
		
		    if (ifaces != null && ifaces.Length > 0)
		    {
		
		        foreach (Type iface in ifaces)
		        {
		
		            Type ienum = FindIEnumerable(iface);
		
		            if (ienum != null) return ienum;
		
		        }
		
		    }
		
		    if (seqType.BaseType != null && seqType.BaseType != typeof(object))
		    {
		
		        return FindIEnumerable(seqType.BaseType);
		
		    }
		
		    return null;
		
		}
	
	}
	
	public class User
	{
		public string Name { get;set;}
		public int Age { get;set;}
		
		public User( string n, int a)
		{
			Name = n;
			Age = a;
		}
		
		
	}
	
	public class MyQueryProvider : QueryProvider
	{
	
		private List<User> myList;
		
		private int currentIndex = 0;
		public MyQueryProvider( ):base()
		{

		}


        private IEnumerable<DynamicDictionary> ProcessSearchResultQuery(Expression expression)
        {
            var spExp = new SPExpressionVisitor();

            var query = spExp.ProcessExpression(expression);


            var spSearch = new SPSearch();

            var res = spSearch.PerformSearch(query);

            return res;
        }

		public override object Execute(Expression expression)
		{
		
			
			MethodCallExpression call = expression as MethodCallExpression;
            if (call != null)
            {
           
				Console.WriteLine("meth " + call.Method.Name);
			}
			
			Type elementType = TypeSystem.GetElementType(expression.Type);
			
			var exp = new SPExpressionVisitor(  );
			

			// hacky way to handle multiple return types.
            if (elementType.Name == "DynamicDictionary")
			{
                var l = ProcessSearchResultQuery(expression);
				
				return l;
			}
			
			// can do it another way other than name?
			if (elementType.Name == "String")
			{
				var l = new List<string>();
				
				foreach (var i in myList)
				{
					l.Add( i.Name  );	
				}
				
				return l;
				
			}
			
			
			
			return null;
		}
		
		public override string GetQueryText(Expression expression)
        {
            return "no idea";
        }
		
	}
	
	public class Program
	{
        public static void Mainxx(string[] args)
        {

            dynamic myobj = new DynamicDictionary();

            myobj.mystring = "stringy";
            myobj.myint = 123;

            var n = myobj.mystring;
            var i = myobj.myint;

            Console.WriteLine( n );
            Console.WriteLine( i );


        }

		public static void Main( string[] args)
		{

            dynamic a = new ExpandoObject();

            a.foo = "ken";
            a.blah = "lll";

            a.blah = "ddd";

			QueryProvider qp = new MyQueryProvider(  );

            Query<DynamicDictionary> q = new Query<DynamicDictionary>(qp);

            var res = from r in q where r.Query == "cat" && r.URL == "sat" select r;

			
			if ( res != null)
			{
				foreach (var i in res )
				{
					Console.WriteLine("i is " + i.Title );		
				}

                Console.WriteLine("press a key.... I dare you....");
                Console.ReadKey();
			}
			
 		}
	}
}
