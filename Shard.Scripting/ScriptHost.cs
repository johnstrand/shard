using Esprima;
using Esprima.Ast;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Shard.Scripting
{
    public static class ScriptHost
    {
        private static readonly ConcurrentDictionary<string, Expression> parserCache = new ConcurrentDictionary<string, Expression>();

        public static string Evaluate(string expression, Dictionary<string, string> context)
        {
            var parsedExpression = parserCache.GetOrAdd(expression.Trim(), ParseExpression);
            return EvaluateExpression(parsedExpression, context)?.ToString();
        }

        private static object EvaluateExpression(INode expression, Dictionary<string, string> context)
        {
            if (expression is AssignmentExpression assignment)
            {
                if (assignment.Left is Identifier id)
                {
                    return context[id.Name] = EvaluateExpression(assignment.Right, context)?.ToString();
                }
                else if (assignment.Left is ArrayPattern arr)
                {
                    var identifiers = arr.Elements.Select(e => e.As<Identifier>().Name).ToList();
                    var values = EvaluateExpression(assignment.Right, context).As<List<object>>();
                    if (identifiers.Count != values.Count)
                    {
                        throw new ArgumentException($"Array assignment value mismatch, expected {identifiers.Count}, found {values.Count}");
                    }

                    foreach (var (First, Second) in identifiers.Zip(values))
                    {
                        context[First] = Second?.ToString();
                    }

                    return null;
                }
                else
                {
                    throw new ArgumentException($"Assignment target {assignment.Left.Type} is not a valid target");
                }
            }
            else if (expression is CallExpression call)
            {
                var target = call.Callee.As<StaticMemberExpression>();
                var method = EvaluateExpression(target.Property, context).As<Identifier>().Name;
                var source = EvaluateExpression(target.Object, context);
                var arguments = call.Arguments.Select(arg => EvaluateExpression(arg, context)).ToList();
                if (source is Identifier id)
                {
                    source = context[id.Name];
                }
                if (ScriptCommand.HasCommand(method))
                {
                    return ScriptCommand.Execute(method, arguments.Prepend(source).ToList());
                }
                throw new NotSupportedException($"Method {method} is not supported");
            }
            else if (expression is Identifier identifier)
            {
                return identifier;
            }
            else if (expression is Literal literal)
            {
                return literal.Value;
            }
            else if (expression is SequenceExpression sequence)
            {
                return sequence.Expressions.Select(expr => EvaluateExpression(expr, context)).Last();
            }
            else if (expression is ArrayExpression arr)
            {
                return arr.Elements.Select(e => EvaluateExpression(e, context)).ToList();
            }
            else
            {
                return null;
            }
        }

        private static Expression ParseExpression(string expression) => new JavaScriptParser(expression).ParseExpression();
    }
}