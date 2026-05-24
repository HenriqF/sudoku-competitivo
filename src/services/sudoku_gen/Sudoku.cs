using System;
using System.Drawing;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json.Serialization;
using System.Xml;

using contracts;

namespace Sudoku;

public class Sudoku
{
    public static int board_size;
    public static int blockheight;
    public static int blockwidth;
    public static string[] numeros = new string[0];
    public static string[,] board = new string[0, 0];
    public static string[,] solution = new string[0, 0];


    static void copy_references(string[,] original, string[,] copy)
    {
        for (int i = 0 ; i < original.GetLength(0); i++)
        {
            for (int j = 0; j < original.GetLength(1); j++)
            {
                copy[i,j] = original[i,j];
            }
        }
    }


    static private int solve_count_solutions(string[,] board, string[,] solution, int random_numbers)
    {
        List<HashSet<String>> rows = new List<HashSet<String>>(board_size); 
        List<HashSet<String>> columns = new List<HashSet<String>>(board_size); 
        List<HashSet<String>> blocks = new List<HashSet<String>>(board_size); 
        for (int i = 0; i < board_size ; i++)
        {
            rows.Add(new HashSet<String>());
            columns.Add(new HashSet<String>());
            blocks.Add(new HashSet<String>());
        }
        List<(int, int)> empty = new List<(int, int)>();

        for (int r = 0 ; r < board_size; r++)
        {
            for (int c = 0; c < board_size; c++)
            {
                if (board[r, c] == "_") empty.Add((r,c));
                else
                {
                    string ch = board[r, c];
                    rows[r].Add(ch);
                    columns[c].Add(ch);
                    blocks[(r/blockheight)*blockheight+(c/blockwidth)].Add(ch);
                }
            }
        }


        int solutions = 0;
        string[,] first_solution = new string[board_size, board_size];
        string[] n = new string[board_size];
        for (int i = 0 ; i < board_size; i++) n[i] = numeros[i];


        int solve_from(int index)
        {
            if (index == empty.Count)
            {
                solutions++;
                if (solutions == 1) copy_references(solution, first_solution);
                else if (solutions >= 2) return 1;
                return 0;
            }

            int r = empty[index].Item1;
            int c = empty[index].Item2;
            int b = (r/blockheight)*blockheight+(c/blockwidth);

            if (random_numbers == 1)
            {
                Random rng = new Random();
                int len = n.Length;
                for (int i = len-1; i > 0; i--)
                {
                    int j = rng.Next(i+1);
                    string temp = n[i];
                    n[i] = n[j];
                    n[j] = temp;
                }
            }
            
            foreach(string ch in n)
            {
                if ( !rows[r].Contains(ch) && !columns[c].Contains(ch) && !blocks[b].Contains(ch) )
                {
                    solution[r, c] = ch;
                    rows[r].Add(ch);
                    columns[c].Add(ch);
                    blocks[b].Add(ch);

                    if (solve_from(index+1) == 1) return 1;

                    solution[r, c] = "_";
                    rows[r].Remove(ch);
                    columns[c].Remove(ch);
                    blocks[b].Remove(ch);
                }
            }

            return 0;
        }

        int result = solve_from(0);
        copy_references(first_solution, solution);
        return result;
    }

    static private void new_valid_boards(string[,] board, string[,] solution)
    {
        string[,] new_board = new string[board_size, board_size];
        string[,] master = new string[board_size, board_size];
        for (int r = 0; r < board_size; r++)
        {
            for (int c = 0; c < board_size; c++)
            {
                new_board[r,c] = "_";
                master[r,c] = "_"; 
            }
        }
        solve_count_solutions(new_board, master, 1);
        copy_references(master, solution);


        Random random = new Random();

        int count = 0;
        int size_sqr = board_size*board_size;
        int to_remove = random.Next((int)(size_sqr * 0.55), (int)(size_sqr * 0.60) + 1);

        List<(int, int)> not_removed = new List<(int, int)>();
        for (int r = 0; r < board_size; r++)
        {
            for (int c = 0; c < board_size; c++)
            {
                not_removed.Add((r,c));
            }
        }
        for (int i = not_removed.Count-1; i > 0; i--)
        {
            int j = random.Next(i+1);
            (int, int) temp = not_removed[i];
            not_removed[i] = not_removed[j];
            not_removed[j] = temp;
        }

        while (count < to_remove)
        {
            while (true)
            {
                if (not_removed.Count == 0)break;

                int index = not_removed.Count-1;
                int r = not_removed[index].Item1;
                int c = not_removed[index].Item2;
                not_removed.RemoveAt(index);

                string[,] new_master = new string[board_size, board_size];
                copy_references(master, new_master);
                new_master[r,c] = "_";

                if (solve_count_solutions(new_master, new_master, 0) == 0)
                {
                    master[r,c] = "_";
                    break;
                }

            }
            count += 1;
        }


        copy_references(master, board);
    }

    public void show_boards()
    {
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                Console.Write(board[i, j] + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("\n");

        for (int i = 0; i < solution.GetLength(0); i++)
        {
            for (int j = 0; j < solution.GetLength(1); j++)
            {
                Console.Write(solution[i, j] + " ");
            }
            Console.WriteLine();
        }
    }

    public static string[] new_boards()
    {
        new_valid_boards(board, solution);

        char[] b = new char[board_size*board_size];
        char[] s = new char[board_size*board_size];
        int index = 0;

        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                b[index] = char.Parse(board[i , j]);
                s[index++] = char.Parse(solution[i, j]);
            }
        }

        string newboard = new string((char[]) b);
        string newsolution = new string((char[]) s);
        string[] answer = new string[] { newboard, newsolution };

        return answer;
    }


    static void Main(string[] args)
    {
        Console.WriteLine("===============");
        Console.WriteLine("SUDOKU SUDOKU");
        Console.WriteLine("===============");

        board_size = 6;
        blockwidth = 3;
        blockheight = 2;
        numeros = new string[board_size];
        for (int i = 0; i < board_size; i++) numeros[i] = (i + 1).ToString();

        board = new string[board_size, board_size];
        solution = new string[board_size, board_size];     



        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.UseHttpsRedirection();

        app.MapGet("/online", () =>
        {
           return Results.Ok("sim!"); 
        });

        app.MapGet("/new", () =>
        {
            return Results.Ok(new new_sudokus(new_boards()));
        });

        app.Run();
    }

}

public partial class Program { }