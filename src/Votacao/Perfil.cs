namespace Conde.Votacao;

/// <summary>
/// Uma CEDULA: a ordem de preferencia de um eleitor sobre os candidatos.
///
/// A ordem e completa e estrita: o eleitor ordena todos os candidatos e nao ha
/// empates. Essa restricao e a de Arrow, e ela nao e por conveniencia: sem ela,
/// os teoremas de impossibilidade ficam mais faceis de evitar e menos
/// interessantes.
/// </summary>
public sealed class Cedula
{
    private readonly int[] posicao;

    public Cedula(IEnumerable<int> ordem)
    {
        ArgumentNullException.ThrowIfNull(ordem);
        Ordem = [.. ordem];
        if (Ordem.Distinct().Count() != Ordem.Count)
            throw new ArgumentException("a cedula repete candidato", nameof(ordem));

        posicao = new int[Ordem.Count];
        for (var i = 0; i < Ordem.Count; i++)
        {
            if (Ordem[i] < 0 || Ordem[i] >= Ordem.Count)
                throw new ArgumentException($"candidato {Ordem[i]} fora da faixa", nameof(ordem));
            posicao[Ordem[i]] = i;
        }
    }

    /// <summary>Do preferido para o menos preferido.</summary>
    public IReadOnlyList<int> Ordem { get; }

    public int Candidatos => Ordem.Count;

    /// <summary>Em que lugar da preferencia o candidato esta. Zero e o primeiro.</summary>
    public int Posicao(int candidato) => posicao[candidato];

    /// <summary>Verdadeiro quando este eleitor prefere a a b.</summary>
    public bool Prefere(int a, int b) => posicao[a] < posicao[b];

    /// <summary>O preferido.</summary>
    public int Primeiro => Ordem[0];

    /// <summary>
    /// A mesma cedula com dois candidatos trocados de lugar, para as buscas por
    /// manipulacao.
    /// </summary>
    public Cedula Trocando(int i, int j)
    {
        var nova = Ordem.ToArray();
        (nova[i], nova[j]) = (nova[j], nova[i]);
        return new Cedula(nova);
    }

    public override string ToString() => string.Join(">", Ordem.Select(x => (char)('A' + x)));
}

/// <summary>
/// Um PERFIL: as cedulas de todos os eleitores.
///
/// A pergunta da teoria da escolha social e: dado um perfil, qual candidato
/// vence? Parece que basta contar votos, e a parte interessante e que nao basta:
/// metodos razoaveis discordam no mesmo perfil, e nenhum metodo consegue ter
/// todas as propriedades que se gostaria ao mesmo tempo.
///
/// Este repositorio nao cita esses resultados: ele PROCURA os contraexemplos
/// varrendo todos os perfis pequenos.
/// </summary>
public sealed class Perfil
{
    public Perfil(IEnumerable<Cedula> cedulas)
    {
        ArgumentNullException.ThrowIfNull(cedulas);
        Cedulas = [.. cedulas];
        if (Cedulas.Count == 0) throw new ArgumentException("perfil sem eleitor", nameof(cedulas));

        Candidatos = Cedulas[0].Candidatos;
        foreach (var cedula in Cedulas)
            if (cedula.Candidatos != Candidatos)
                throw new ArgumentException("as cedulas tem conjuntos de candidatos diferentes", nameof(cedulas));
    }

    public IReadOnlyList<Cedula> Cedulas { get; }

    public int Candidatos { get; }

    public int Eleitores => Cedulas.Count;

    /// <summary>Quantos eleitores preferem a a b.</summary>
    public int Quantos(int a, int b) => Cedulas.Count(c => c.Prefere(a, b));

    /// <summary>
    /// Verdadeiro quando a VENCE b num confronto direto, por maioria.
    ///
    /// Empate nao e vitoria, e essa escolha importa: com um numero par de
    /// eleitores os empates sao comuns, e tratar empate como vitoria faria o
    /// vencedor de Condorcet deixar de ser unico.
    /// </summary>
    public bool Vence(int a, int b) => Quantos(a, b) > Quantos(b, a);

    /// <summary>
    /// O perfil com UM eleitor trocando a cedula, para as buscas por
    /// manipulacao.
    /// </summary>
    public Perfil Com(int eleitor, Cedula nova)
    {
        var copia = Cedulas.ToArray();
        copia[eleitor] = nova;
        return new Perfil(copia);
    }

    /// <summary>
    /// O perfil SEM um candidato, para a busca por violacao da independencia de
    /// alternativas irrelevantes.
    ///
    /// A renumeracao e a parte chata e necessaria: tirado o candidato k, os
    /// maiores que ele descem um, e esquecer isso produz cedulas invalidas em
    /// silencio.
    /// </summary>
    public Perfil Sem(int candidato)
    {
        if (Candidatos <= 2) throw new InvalidOperationException("nao da para tirar candidato de um perfil de dois");
        var novas = new List<Cedula>(Eleitores);
        foreach (var cedula in Cedulas)
        {
            var ordem = cedula.Ordem.Where(x => x != candidato)
                                    .Select(x => x > candidato ? x - 1 : x);
            novas.Add(new Cedula(ordem));
        }
        return new Perfil(novas);
    }

    /// <summary>
    /// TODOS os perfis de <paramref name="eleitores"/> eleitores sobre
    /// <paramref name="candidatos"/> candidatos.
    ///
    /// Sao (c!)^e perfis, o que explode rapido: com tres candidatos e tres
    /// eleitores sao 216, com quatro eleitores sao 1.296, e com quatro
    /// candidatos e tres eleitores sao 13.824. Cabe numa varredura, e e nela que
    /// os paradoxos sao ACHADOS em vez de citados.
    /// </summary>
    public static IEnumerable<Perfil> Todos(int candidatos, int eleitores)
    {
        var ordens = Permutacoes(candidatos).Select(x => new Cedula(x)).ToList();
        var escolha = new Cedula[eleitores];

        IEnumerable<Perfil> Andar(int i)
        {
            if (i == eleitores) { yield return new Perfil(escolha.ToArray()); yield break; }
            foreach (var ordem in ordens)
            {
                escolha[i] = ordem;
                foreach (var perfil in Andar(i + 1)) yield return perfil;
            }
        }
        return Andar(0);
    }

    /// <summary>
    /// Todos os perfis, enumerados por CONTAGEM em vez de por sequencia de
    /// cedulas.
    ///
    /// Esta e a diferenca entre conseguir e nao conseguir varrer. Todo metodo
    /// deste repositorio e ANONIMO: ele nao olha QUEM votou o que, so quantos
    /// votaram em cada ordem. Entao dois perfis que diferem por uma permutacao
    /// dos eleitores sao o mesmo perfil para efeito de resultado, e enumerar
    /// sequencias conta cada um varias vezes.
    ///
    /// Enumerando quantos eleitores escolheram cada uma das c! ordens, o espaco
    /// cai de (c!)^e para C(e + c! - 1, c! - 1). Com tres candidatos e dezessete
    /// eleitores, isso e 26.334 em vez de 6^17, que passa de dez trilhoes.
    ///
    /// Isso nao e otimizacao: e o que torna possivel achar a violacao de
    /// monotonicidade do segundo turno, que nao aparece com menos de uma dezena
    /// de eleitores.
    /// </summary>
    public static IEnumerable<Perfil> TodosPorContagem(int candidatos, int eleitores)
    {
        var ordens = Permutacoes(candidatos).Select(x => new Cedula(x)).ToList();
        var quantas = ordens.Count;
        var contagem = new int[quantas];

        IEnumerable<Perfil> Andar(int i, int faltam)
        {
            if (i == quantas - 1)
            {
                contagem[i] = faltam;
                yield return Montar(ordens, contagem);
                yield break;
            }
            for (var quantos = 0; quantos <= faltam; quantos++)
            {
                contagem[i] = quantos;
                foreach (var perfil in Andar(i + 1, faltam - quantos)) yield return perfil;
            }
        }
        return Andar(0, eleitores);
    }

    private static Perfil Montar(IReadOnlyList<Cedula> ordens, int[] contagem)
    {
        var cedulas = new List<Cedula>();
        for (var i = 0; i < ordens.Count; i++)
            for (var k = 0; k < contagem[i]; k++) cedulas.Add(ordens[i]);
        return new Perfil(cedulas);
    }

    public static IEnumerable<List<int>> Permutacoes(int quantos) =>
        Permutar([.. Enumerable.Range(0, quantos)]);

    private static IEnumerable<List<int>> Permutar(List<int> restantes)
    {
        if (restantes.Count <= 1) { yield return restantes; yield break; }
        for (var i = 0; i < restantes.Count; i++)
        {
            var resto = new List<int>(restantes);
            resto.RemoveAt(i);
            foreach (var cauda in Permutar(resto)) yield return [restantes[i], .. cauda];
        }
    }

    /// <summary>Um perfil escrito de forma curta: "ABC,BCA,CAB".</summary>
    public static Perfil De(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var cedulas = texto.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => new Cedula(x.Trim().Select(c => c - 'A')));
        return new Perfil(cedulas);
    }

    public override string ToString() => string.Join(",", Cedulas.Select(x => x.ToString().Replace(">", "")));
}
