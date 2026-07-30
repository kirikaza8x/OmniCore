namespace OmniCore.Shared.Domain.Specifications;

using System.Linq.Expressions;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new();
    private readonly List<string> _includeStrings = new();

    protected BaseSpecification() { }

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        AddCriteria(criteria);
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool IsNoTracking { get; private set; }

    // CS0535 FIX: Implemented IsSatisfiedBy for in-memory evaluation
    public virtual bool IsSatisfiedBy(T entity)
    {
        return Criteria is null || Criteria.Compile()(entity);
    }

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        if (Criteria is null)
        {
            Criteria = criteria;
            return;
        }

        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(Criteria.Parameters[0], parameter);
        var left = leftVisitor.Visit(Criteria.Body);

        var rightVisitor = new ReplaceExpressionVisitor(criteria.Parameters[0], parameter);
        var right = rightVisitor.Visit(criteria.Body);

        Criteria = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => _includes.Add(includeExpression);
    protected void AddInclude(string includeString) => _includeStrings.Add(includeString);
    protected void AddOrderBy(Expression<Func<T, object>> orderByExpression) => OrderBy = orderByExpression;
    protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression) => OrderByDescending = orderByDescExpression;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void ApplyNoTracking() => IsNoTracking = true;

    private sealed class ReplaceExpressionVisitor(Expression oldValue, Expression newValue) : ExpressionVisitor
    {
        public override Expression? Visit(Expression? node) 
            => node == oldValue ? newValue : base.Visit(node);
    }
}