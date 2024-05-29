﻿/* Una.Drawing                                                 ____ ___
 *   A declarative drawing library for FFXIV.                 |    |   \____ _____        ____                _
 *                                                            |    |   /    \\__  \      |    \ ___ ___ _ _ _|_|___ ___
 * By Una. Licensed under AGPL-3.                             |    |  |   |  \/ __ \_    |  |  |  _| .'| | | | |   | . |
 * https://github.com/una-xiv/drawing                         |______/|___|  (____  / [] |____/|_| |__,|_____|_|_|_|_  |
 * ----------------------------------------------------------------------- \/ --- \/ ----------------------------- |__*/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Una.Drawing;

public partial class Node
{
    private readonly Dictionary<string, Node?>      _selectorCache      = [];
    private readonly Dictionary<string, List<Node>> _multiSelectorCache = [];
    private readonly Dictionary<string, Node?>      _findByIdCache      = [];

    /// <summary>
    /// Returns the first node that matches the given query selector, or NULL
    /// if no matching node was found.
    /// </summary>
    /// <param name="querySelectorString">The query selector string.</param>
    /// <returns>A <see cref="Node"/> object or NULL if no such node exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public Node? QuerySelector(string querySelectorString)
    {
        if (_selectorCache.TryGetValue(querySelectorString, out Node? cachedNode)) {
            return cachedNode;
        }

        var querySelectors = QuerySelectorParser.Parse(querySelectorString);

        foreach (var querySelector in querySelectors) {
            Node? node = FindChildrenMatching(this, querySelector, true).FirstOrDefault();

            if (node != null) {
                _selectorCache[querySelectorString] = node;
                return node;
            }
        }

        return _selectorCache[querySelectorString] = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public Node? FindById(string nodeId)
    {
        if (Id == nodeId) return this;

        if (_findByIdCache.TryGetValue(nodeId, out Node? cachedNode)) {
            return cachedNode;
        }

        foreach (var child in _childNodes) {
            var node = child.FindById(nodeId);

            if (node != null) {
                return _findByIdCache[nodeId] = node;
            }
        }

        return _findByIdCache[nodeId] = null;
    }

    /// <summary>
    /// Returns the first node that matches the given query selector and type
    /// argument, or NULL if no such node exists.
    /// </summary>
    /// <param name="querySelectorString">The query selector string.</param>
    /// <returns>An instance of T that inherits <see cref="Node"/> or NULL.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public T? QuerySelector<T>(string querySelectorString) where T : Node
    {
        return QuerySelector(querySelectorString) as T;
    }

    /// <summary>
    /// Returns a list of nodes that match the given query selector.
    /// </summary>
    /// <param name="querySelectorString">The query selector string.</param>
    /// <returns>A list of matching <see cref="Node"/> instances.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<Node> QuerySelectorAll(string querySelectorString)
    {
        if (_multiSelectorCache.TryGetValue(querySelectorString, out List<Node>? cachedNodeList)) {
            return cachedNodeList;
        }

        List<QuerySelector> querySelectors = QuerySelectorParser.Parse(querySelectorString);
        List<Node>          nodeListResult = [];

        foreach (var querySelector in querySelectors) {
            nodeListResult.AddRange(FindChildrenMatching(this, querySelector, true));
        }

        _multiSelectorCache[querySelectorString] = nodeListResult;

        return nodeListResult;
    }

    /// <summary>
    /// Returns a list of nodes that match the given query selector and type
    /// argument.
    /// </summary>
    /// <param name="querySelectorString">The query selector string.</param>
    /// <returns>A list of matching nodes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<T> QuerySelectorAll<T>(string querySelectorString) where T : Node
    {
        return QuerySelectorAll(querySelectorString).Cast<T>().ToList();
    }

    /// <summary>
    /// Clears out the selector query cache. This should always be done when
    /// the list of child nodes has changed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ClearQuerySelectorCache()
    {
        _findByIdCache.Clear();
        _selectorCache.Clear();
        _multiSelectorCache.Clear();

        // Clear cache for parent nodes as well, since querying for nodes is
        // always a recursive operation.
        ParentNode?.ClearQuerySelectorCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static List<Node> FindChildrenMatching(Node node, QuerySelector querySelector, bool recursive)
    {
        List<Node> nodeListResult = [];

        foreach (var child in node._childNodes) {
            if (!MatchesQuerySelector(child, querySelector)) {
                continue;
            }

            if (querySelector.DirectChild != null) {
                var result = FindChildrenMatching(child, querySelector.DirectChild, false);
                if (result.Count > 0) nodeListResult.AddRange(result);
                continue;
            }

            if (querySelector.NestedChild != null) {
                var result = FindChildrenMatching(child, querySelector.NestedChild, true);
                if (result.Count > 0) nodeListResult.AddRange(result);
                continue;
            }

            nodeListResult.Add(child);
        }

        if (recursive && (querySelector.IsFirstSelector || nodeListResult.Count == 0) && node._childNodes.Count > 0) {
            foreach (var child in node._childNodes) {
                nodeListResult.AddRange(FindChildrenMatching(child, querySelector, true));
            }
        }

        return nodeListResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool MatchesQuerySelector(Node node, QuerySelector querySelector)
    {
        if (querySelector.Identifier != null && node.Id != querySelector.Identifier) {
            return false;
        }

        if (querySelector.ClassList.Count > 0) {
            if (node.ClassList.Count == 0) return false;

            foreach (string className in querySelector.ClassList) {
                if (!node.ClassList.Contains(className)) {
                    return false;
                }
            }
        }

        if (querySelector.TagList.Count > 0) {
            if (node.TagsList.Count == 0) return false;

            foreach (string tag in querySelector.TagList) {
                if (!node.TagsList.Contains(tag)) {
                    return false;
                }
            }
        }

        return true;
    }
}
