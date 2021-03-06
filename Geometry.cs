using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Geometry
{
	public const float threshold = 0.01f;
	/// <summary>
	/// Shrink a polygon with anti-clockwise winding by moving its edges inwards.
	/// Outputs a list of anti-clockwise winded polygons and returns a success flag.
	/// </summary>
	public static bool OffsetPolygonInwards(
		IList<Vector2> polygonPoints, float offset, out List<List<Vector2>> polygons )
	{
		polygons = new List<List<Vector2>>();
		
		// polygons = SimpleScale(polygonPoints.ToList(), offset);
		polygons = ShrinkEdges(polygonPoints.ToList(), offset);
		
		bool success = true;

		return success;
	}

	public static float Cross2(Vector2 v, Vector2 w)
		=> Vector3.Cross(new Vector3(v.x, v.y), new Vector3(w.x, w.y)).z;

	public static List<(int, int, int)> Triples(int n)
	{
		var p = Enumerable.Range(0, n).ToList();
		var a = Enumerable.Range(0, n).ToList();
		var res = new List<(int, int, int)>();
		foreach (int i in p){
			foreach (int j in a){
				var k = (j + 1) % n;
				if (j != (i - 1 + n) % n && k != (i + 1) % n)
					res.Add((i, j, k));
			}
		}
		return res;
	}

	public static List<int> SplitPolyL(int pi, int ai, int n)
		=> Enumerable.Range(pi, n).Select(i => i % n).TakeWhile(i => i != ai).Append(ai).ToList();

	public static List<int> SplitPolyR(int pi, int bi, int n)
		=> Enumerable.Range(bi, n).Select(i => i % n).TakeWhile(i => i != pi).Append(pi).ToList();
	
	public static List<float> RootsDeg2(float a, float b, float c)
	{
		var d = b * b - 4 * a * c;
		if (d < 0) return new List<float>();
		var e = - b / (2 * a);
		if (d == 0) return new List<float>() { e };
		var x = (float) (e + Math.Sqrt(d) / (2 * a));
		var y = (float) (e - Math.Sqrt(d) / (2 * a));
		return new List<float>() { x, y };
	}

	public static List<float> LineSegCol(Vector2 a0, Vector2 b0, Vector2 va, Vector2 vb)
	{
		var ca = Cross2(va, vb);
		var cb = Cross2(a0, vb) - Cross2(b0, va);
		var cc = Cross2(a0, b0);
		var roots = RootsDeg2(ca, cb, cc);
		Func<float, bool> cond2 = t => Vector2.Dot(a0 + va * t, b0 + vb * t) < threshold;
		return roots.Where(t => t > -threshold && cond2(t)).ToList();
	}

	public static List<List<Vector2>> SimpleScale(IEnumerable<Vector2> polyIn, float offset)
		=> new List<List<Vector2>>() { polyIn.Select(x => x * offset).ToList() };
	
	public static bool Between(float a, float b, float x)
		=> a < x && x < b;

	public static bool Clockwise(List<Vector2> vs)
	{
		var n = vs.Count;
		var us = vs.Select((x, i) => vs[(i + 1) % n]).ToList();
		return 0 < vs.Select((v, i) => (us[i].x - v.x) * (us[i].y + v.y)).Sum();
	}

	/// <summary>
	/// assumes that p is counter-clockwisely oriented
	/// </summary>
	public static List<List<Vector2>> ShrinkEdges(List<Vector2> p, float t, int iter = 0)
	{
		const int maxIter = 100;
		int n = p.Count();
		if (iter > maxIter || n < 3)
			return new List<List<Vector2>>();
		iter ++;
		var nl = p.Select((x, i) => p[(i - 1 + n) % n]).ToList();
		var nr = p.Select((x, i) => p[(i + 1) % n]).ToList();
		var vl = nl.Select((x, i) => (x - p[i])).ToList();
		var vr = nr.Select((x, i) => (x - p[i])).ToList();
		var vln = vl.Select(x => x.normalized).ToList();
		var vrn = vr.Select(x => x.normalized).ToList();
		var cp = vln.Select((x, i) => -Cross2(x, vrn[i])).ToList();
		var vd = vln.Select((x, i) => (x + vrn[i]) / cp[i]).ToList();
		Func<float, List<Vector2>> pt = t_ => p.Select((v, i) => v + vd[i] * t_).ToList();
		var ptt = pt(t);
		if (n == 3) {
			var test = Vector2.Dot(((ptt[1] - ptt[0]) + (ptt[2] - ptt[0])), vd[0]);
			if (test > 0) return new List<List<Vector2>>() { ptt };
			else return new List<List<Vector2>>();
		}
		var triples = Triples(n);
		var cols = triples.SelectMany((trpl) => {
			var (i, j, k) = trpl;
			var rs = LineSegCol(
				p[j] - p[i],
				p[k] - p[i],
				vd[j] - vd[i],
				vd[k] - vd[i]);
			return rs.Select(r => (i, j, k, r, pt(r)[i])).ToList();
		});
		if (cols.Count() == 0) return new List<List<Vector2>>();
		// TODO: above line should actually throw error or be handled better
		// maybe by moving the logic in case (n == 3) to here instead
		else {
			var (fpi, fai, fbi, ft, fcp) = cols.OrderBy(x => x.Item4).First();
			if (ft > t) return new List<List<Vector2>>() { pt(t) };
			if (n == 3) return new List<List<Vector2>>();
			var pft = pt(ft);
			var polyL = SplitPolyL(fpi, fai, n).Select(i => pft[i]).ToList();
			var polyR = SplitPolyR(fpi, fbi, n).Select(i => pft[i]).ToList();
			return Enumerable.Concat(
				ShrinkEdges(polyL, t - ft, iter), ShrinkEdges(polyR, t - ft, iter)
			).ToList();
		}
	}
}

/* trash

public static bool Clockwise(List<Vector2> vs)
{
  var n = vs.Count;
  var minY = vs.Select(x => x.y).Min();
  var minYs = vs.Select((x, i) => (x, i)).Where(x => x.Item1.y == minY);
	var minX = minYs.Select(x => x.Item1.x).Min();
	var (a, i) = minYs.Where(x => x.Item1.x == minX).First();
  var b = vs[(i - 1 + n) % n];
  var c = vs[(i + 1) % n];
  return Cross2(b - a, c - a) > 0;
}

public static float Cross2_(Vector2 v, Vector2 w)
	=> (v.x * w.y - v.y * w.x);

*/