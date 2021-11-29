using CodeArts.Middleware;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Emit.Tests
{
    public class Entry
    {
        public int Id { get; set; }
    }

    [TestClass]
    public class Tests
    {
        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 _) => f.Method;
        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 _, T2 _2) => f.Method;

        private static Expression GetExpression<TResult>(Expression<Func<Entry, TResult>> expression)
        {
            return expression;
        }

        [TestMethod]
        public void Test1()
        {
#if NET461
            var m = new ModuleEmitter(true);
#else
            var m = new ModuleEmitter();
#endif
            var classType = m.DefineType("test", TypeAttributes.Public);
            var method = classType.DefineMethod("Test", MethodAttributes.Public, typeof(int));

            var pI = method.DefineParameter(typeof(int), ParameterAttributes.None, "i");
            var pJ = method.DefineParameter(typeof(int), ParameterAttributes.None, "j");

            var b = Variable(typeof(Expression));

            method.Append(Assign(b, Constant(GetExpression(entry => entry.Id))));

            var switchAst = Switch(Constant(1));

            switchAst.Case(Constant(1))
                .Append(IncrementAssign(pI));

            switchAst.Case(Constant(2))
                .Append(AddAssign(pI, Constant(5)));

            var constantAst2 = Constant(1, typeof(object));

            var switchAst2 = Switch(constantAst2);

            var stringAst = Variable(typeof(string));
            var intAst = Variable(typeof(int));

            switchAst2.Case(stringAst);
            switchAst2.Case(intAst)
                .Append(Assign(pI, intAst));

            var switchAst3 = Switch(Constant("ABC"), DecrementAssign(pI), typeof(void));

            switchAst3.Case(Constant("A"))
                .Append(IncrementAssign(pI));

            switchAst3.Case(Constant("B"))
                .Append(AddAssign(pI, Constant(5)));

            method.Append(switchAst)
                .Append(switchAst2)
                .Append(switchAst3)
                .Append(Return(Condition(GreaterThanOrEqual(pI, pJ), pI, pJ)));

            var type = classType.CreateType();
#if NET461
            m.SaveAssembly();
#endif
        }

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters)
        {
            return Expression.Lambda<TDelegate>(body, parameters);
        }

        public static IQueryable<TResult> Select<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            return Queryable.Select(source, selector);
        }

        [TestMethod]
        public void TestLinq()
        {
#if NET461
            var m = new ModuleEmitter(true);
#else
            var m = new ModuleEmitter();
#endif
            var classType = m.DefineType("linq", TypeAttributes.Public);
            var method = classType.DefineMethod("Query", MethodAttributes.Public, typeof(void));

            var pI = method.DefineParameter(typeof(int), ParameterAttributes.None, "i");
            var pJ = method.DefineParameter(typeof(int), ParameterAttributes.None, "j");

            var type = typeof(Entry);

            var arg = Variable(typeof(ParameterExpression));
            var argProperty = Variable(typeof(MemberExpression));

            var callArg = Call(typeof(Expression).GetMethod(nameof(Expression.Parameter), new Type[] { typeof(Type) }), Constant(type));
            var callProperty = Call(typeof(Expression).GetMethod(nameof(Expression.Property), new Type[] { typeof(Expression), typeof(string) }), arg, Constant("Id"));
            //var callBlock = Call(typeof(Expression).GetMethod(nameof(Expression.Block), new Type[] { typeof(IEnumerable<ParameterExpression>), typeof(IEnumerable<Expression>) }));

            method.Append(Assign(arg, callArg));
            method.Append(Assign(argProperty, callProperty));

            var variable_variables = Variable(typeof(ParameterExpression[]));

            var variables = NewArray(1, typeof(ParameterExpression));

            method.Append(Assign(variable_variables, variables));

            method.Append(Assign(ArrayIndex(variable_variables, 0), arg));

            var constantI = Call(typeof(Expression).GetMethod(nameof(Expression.Constant), new Type[] { typeof(object) }), Convert(pI, typeof(object)));

            var equalMethod = Call(typeof(Expression).GetMethod(nameof(Expression.Equal), new Type[] { typeof(Expression), typeof(Expression) }), argProperty, constantI);

            var lamdaMethod = typeof(Tests).GetMethod(nameof(Tests.Lambda))
                .MakeGenericMethod(typeof(Func<Entry, bool>));

            var whereLambda = Variable(typeof(Expression<Func<Entry, bool>>));

            method.Append(Assign(whereLambda, Call(lamdaMethod, equalMethod, variable_variables)));

            method.Append(ReturnVoid());

            classType.CreateType();
#if NET461
            m.SaveAssembly();
#endif

            // ���ɴ������£�
            /**
            public void Query(int i, int j)
            {
	            ParameterExpression parameterExpression = Expression.Parameter(typeof(Entry));
	            MemberExpression left = Expression.Property(parameterExpression, "Id");
	            Expression<Func<Entry, bool>> expression = Tests.Lambda<Func<Entry, bool>>(parameters: new ParameterExpression[1]
	            {
		            parameterExpression
	            }, body: Expression.Equal(left, Expression.Constant(i)));
            }
             */
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
        public class DependencyInterceptAttribute : InterceptAttribute
        {
            static DependencyInterceptAttribute()
            {

            }
            public override void Run(InterceptContext context, Intercept intercept)
            {
                intercept.Run(context);
            }

            public override T Run<T>(InterceptContext context, Intercept<T> intercept)
            {
                if (context.Main.Name == nameof(IDependency.AopTestByRef))
                {
                    context.Inputs[1] = -10;

                    return default;
                }

                return intercept.Run(context);
            }

            public override Task RunAsync(InterceptContext context, InterceptAsync intercept)
            {
                return intercept.RunAsync(context);
            }

            public override Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept)
            {
                return intercept.RunAsync(context);
            }
        }

        /// <inheritdoc />
        [DependencyIntercept]
        public interface IDependency
        {
            bool Flags { get; set; }

            /// <inheritdoc />
            bool AopTest();

            [DependencyIntercept]
            bool AopTestByRef(int i, ref int j);

            //[DependencyIntercept]
            bool AopTestByOut(int i, out int j);

            T Get<T>() where T : struct;

            Task<T> GetAsync<T>() where T : new();
        }

        /// <inheritdoc />
        public class Dependency : IDependency
        {
            public bool Flags { get; set; }

            /// <inheritdoc />
            public bool AopTestByRef(int i, ref int j)
            {
                try
                {
                    return (i & 1) == 0;
                }
                finally
                {
                    j = i * 5;
                }

            }

            /// <inheritdoc />
            public virtual bool AopTestByOut(int i, out int j)
            {
                j = 1;

                return (i & 1) == 0;
            }

            public bool AopTest() => true;

            public T Get<T>() where T : struct => default;

            public Task<T> GetAsync<T>() where T : new()
            {
                return Task.FromResult(new T());
            }
        }

        public class DependencyP : Dependency
        {
            private readonly Dependency dependencyz;

            public DependencyP(Dependency dependencyz)
            {
                this.dependencyz = dependencyz;
            }

            public override bool AopTestByOut(int i, out int j)
            {
                return dependencyz.AopTestByOut(i, out j);
            }
        }

        [DependencyIntercept]
        public interface IDependency<T> where T : class
        {
            T Clone(T obj);

            T Copy(T obj);

            T2 New<T2>() where T2 : T, new();
        }

        public class Dependency<T> : IDependency<T> where T : class
        {
            private static readonly Type __destinationProxyType__;

            public T Clone(T obj)
            {
                //... ��¡���߼���
                return obj;
            }

            public T Copy(T obj)
            {
                //... ��¡���߼���
                return obj;
            }

            public T2 New<T2>() where T2 : T, new()
            {
                return new T2();
            }

            static Dependency()
            {
                __destinationProxyType__ = typeof(Dependency<T>);
            }
        }

        public interface IDependencyA
        {
            bool AopTestByRef(int i, ref int j);
        }

        [DependencyIntercept]
        public class DependencyA : IDependencyA
        {
            public bool Flags { get; set; }

            /// <inheritdoc />
            public virtual bool AopTestByRef(int i, ref int j)
            {
                try
                {
                    return (i & 1) == 0;
                }
                finally
                {
                    j = i * 5;
                }

            }
        }

        [TestMethod]
        public void AopTest()
        {
            var services = new ServiceCollection();

            var serviceProvider = services.AddTransient<IDependency, Dependency>()
                 .AddSingleton<Dependency>()
                 .AddTransient(typeof(IDependency<>), typeof(Dependency<>))
                 .AddScoped<DependencyA>()
                 .UseMiddleware()
                 .BuildServiceProvider();

            IDependency dependency = serviceProvider.GetService<IDependency>();
            IDependency<IDependency> dependency2 = serviceProvider.GetService<IDependency<IDependency>>();

            var dependencyA = serviceProvider.GetService<DependencyA>();

            int i = -10;

            int j = +i;

            dependency.Flags = true;

            _ = dependency.AopTestByRef(3, ref j);

            _ = dependency.AopTestByOut(4, out j);

            _ = dependency.Get<long>();

            _ = dependency.GetAsync<Dependency>().GetAwaiter().GetResult();

            _ = dependency2.Clone(dependency);
            _ = dependency2.Copy(dependency);

            _ = dependency2.New<Dependency>();

            dependencyA.AopTestByRef(i, ref j);
        }

        public class CreateContactsCommand : IRequest<bool>
        {
            /// <summary>
            /// ����
            /// </summary>
            public string ContactsName { get; set; } = string.Empty;

        }

        [DependencyIntercept]
        public class DependencyA1 : IRequestHandler<CreateContactsCommand, bool>
        {
            public Task<bool> Handle(CreateContactsCommand request, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
        }
#if NETSTANDARD2_1
        [TestMethod]
        public void MediatRTest()
        {
            var services = new ServiceCollection();

            services.AddMediatR(System.Reflection.Assembly.GetCallingAssembly());

            var serviceProvider = services
                 .AddScoped<IRequestHandler<CreateContactsCommand, bool>, DependencyA1>()
                 .UseMiddleware()
                 .BuildServiceProvider();

            var requestHandler = serviceProvider.GetService<IRequestHandler<CreateContactsCommand, bool>>();

            requestHandler.Handle(new CreateContactsCommand { }, default);
        }
#endif
    }
}