﻿#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using CodeArts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.LinqAsync
{
    /// <summary>
    /// 异步查询扩展。
    /// </summary>
    public static class QueryableAsyncExtentions
    {
        /// <summary>
        /// 转分页数据。
        /// </summary>
        /// <typeparam name="T">源。</typeparam>
        /// <param name="source">源。</param>
        /// <param name="page">页码（索引从“0”开始）。</param>
        /// <param name="size">分页条数。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public static async Task<PagedList<T>> ToListAsync<T>(this IQueryable<T> source, int page, int size, CancellationToken cancellationToken = default)
        {
            var count_task = source
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var result_task = source
                .Skip(size * page)
                .Take(size)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedList<T>(await result_task, page, size, await count_task);
        }
    }
}
#endif