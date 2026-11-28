using System;
using System.Collections.Generic;
using Pausa = PickABoo.MenuPausa.MenuPausa;
using PickABoo.Transiciones;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace PickABoo.Sonido
{
    internal sealed class PisadasXR : MonoBehaviour
    {
        const float DistanciaPorPaso = 0.55f;
        const float IntervaloMinimo = 0.51f;
        const float MargenEntreClips = 0.04f;
        const float VelocidadMinima = 0.19f;

        readonly List<MeshRenderer> m_Alfombras = new();
        readonly RaycastHit[] m_Suelo = new RaycastHit[24];
        readonly int[] m_UltimasVariantes = { -1, -1, -1 };
        XROrigin m_Origen;
        TeleportationProvider m_Teletransporte;
        Pausa m_Pausa;
        Vector3 m_PosicionAnterior;
        float m_Distancia;
        float m_UltimoMovimiento;
        float m_SiguientePaso;
        bool m_Sincronizado;

        internal bool Activas { get; set; } = true;
        bool Bloqueadas => !Activas || Pausa.Bloqueado || (m_Pausa && m_Pausa.Abierto) || CargadorEscenas.Cargando;

        enum Superficie { Madera, Baldosa, Alfombra }

        void Awake()
        {
            m_Origen = GetComponent<XROrigin>();
            m_Teletransporte = GetComponentInChildren<TeleportationProvider>(true);
            foreach (var raiz in gameObject.scene.GetRootGameObjects())
            {
                if (!m_Pausa) m_Pausa = raiz.GetComponentInChildren<Pausa>();
                foreach (var malla in raiz.GetComponentsInChildren<MeshRenderer>())
                    if (malla.sharedMaterial && malla.sharedMaterial.name.StartsWith("Rugs_", StringComparison.OrdinalIgnoreCase))
                        m_Alfombras.Add(malla);
            }
        }

        void OnEnable()
        {
            if (!m_Teletransporte) return;
            m_Teletransporte.locomotionStarted += IniciarTeletransporte;
            m_Teletransporte.locomotionEnded += FinalizarTeletransporte;
        }

        void OnDisable()
        {
            if (m_Teletransporte)
            {
                m_Teletransporte.locomotionStarted -= IniciarTeletransporte;
                m_Teletransporte.locomotionEnded -= FinalizarTeletransporte;
            }
            m_Sincronizado = false;
        }

        void IniciarTeletransporte(LocomotionProvider _)
        {
            m_Sincronizado = false;
            if (!Bloqueadas && m_Origen.Camera)
            {
                var cabeza = m_Origen.Camera.transform;
                AudioDemo.Efecto("sonidoTransicion", cabeza.position, 0.32f, 1.35f, 0.45f, seguir: cabeza);
            }
        }

        void FinalizarTeletransporte(LocomotionProvider _) => m_Sincronizado = false;

        void LateUpdate()
        {
            if (!m_Origen.Camera) return;
            Vector3 posicion = m_Origen.Camera.transform.position;
            posicion.y = transform.position.y;
            Vector3 delta = posicion - m_PosicionAnterior;
            m_PosicionAnterior = posicion;

            if (!m_Sincronizado || Bloqueadas || delta.sqrMagnitude > 0.65f * 0.65f)
            {
                m_Sincronizado = true;
                m_Distancia = 0f;
                return;
            }

            float distancia = new Vector2(delta.x, delta.z).magnitude;
            if (distancia < VelocidadMinima * Time.deltaTime)
            {
                if (Time.time - m_UltimoMovimiento > 0.18f) m_Distancia = 0f;
                return;
            }

            m_UltimoMovimiento = Time.time;
            m_Distancia = Mathf.Min(DistanciaPorPaso, m_Distancia + distancia);
            if (m_Distancia < DistanciaPorPaso || Time.time < m_SiguientePaso) return;
            if (!BuscarSuelo(posicion, out var suelo)) return;

            m_Distancia = 0f;
            var superficie = IdentificarSuperficie(suelo);
            int indice = (int)superficie;
            int ultima = m_UltimasVariantes[indice];
            int variante = UnityEngine.Random.Range(0, ultima < 0 ? 4 : 3);
            if (ultima >= 0 && variante >= ultima) variante++;
            m_UltimasVariantes[indice] = variante;
            var clip = AudioDemo.Clip($"sonidoPisada{superficie}{variante + 1}");
            float tono = UnityEngine.Random.Range(0.95f, 1.05f);
            float duracion = clip ? clip.length / tono : 0f;
            m_SiguientePaso = Time.time + Mathf.Max(IntervaloMinimo, duracion + MargenEntreClips);
            AudioDemo.Efecto(clip, suelo.point + Vector3.up * 0.08f,
                UnityEngine.Random.Range(0.36f, 0.46f), tono, 0.30f);
        }

        bool BuscarSuelo(Vector3 posicion, out RaycastHit suelo)
        {
            suelo = default;
            float masCerca = float.PositiveInfinity;
            int cantidad = Physics.RaycastNonAlloc(posicion + Vector3.up * 0.45f, Vector3.down, m_Suelo,
                0.95f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                var impacto = m_Suelo[i];
                if (impacto.normal.y < 0.55f || impacto.transform.IsChildOf(transform) || impacto.distance >= masCerca) continue;
                suelo = impacto;
                masCerca = impacto.distance;
            }
            return suelo.collider;
        }

        Superficie IdentificarSuperficie(RaycastHit suelo)
        {
            foreach (var alfombra in m_Alfombras)
            {
                var limites = alfombra.bounds;
                limites.Expand(new Vector3(0f, 0.24f, 0f));
                if (limites.Contains(suelo.point)) return Superficie.Alfombra;
            }

            if (suelo.collider.TryGetComponent<Renderer>(out var malla) && malla.sharedMaterial)
            {
                string material = malla.sharedMaterial.name;
                if (material.StartsWith("Rugs_", StringComparison.OrdinalIgnoreCase)) return Superficie.Alfombra;
                if (material.StartsWith("Floor_Tiles", StringComparison.OrdinalIgnoreCase) ||
                    material.StartsWith("Floor_Bathroom", StringComparison.OrdinalIgnoreCase)) return Superficie.Baldosa;
            }

            string nombre = suelo.collider.name;
            return nombre.StartsWith("Suelo_", StringComparison.OrdinalIgnoreCase) &&
                (nombre.Contains("Kitchen", StringComparison.OrdinalIgnoreCase) || nombre.Contains("Bathroom", StringComparison.OrdinalIgnoreCase))
                ? Superficie.Baldosa : Superficie.Madera;
        }
    }
}
