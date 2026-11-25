using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace PickABoo.MenuPausa
{
    /// <summary>
    /// Aplica al XR Origin las opciones de <see cref="GestorPreferencias"/>:
    /// tipo de giro (por saltos o continuo) y viñeta de confort al moverse y girar.
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public class AplicarPreferencias : MonoBehaviour
    {
        [Tooltip("Viñeta de confort de XRIT (se añade a la cámara si el XR Origin no la trae)")]
        public GameObject prefabVineta;

        ControllerInputActionManager[] m_Mandos;
        TunnelingVignetteController m_Vineta;
        readonly List<LocomotionVignetteProvider> m_Proveedores = new List<LocomotionVignetteProvider>();

        void Start()
        {
            m_Mandos = GetComponentsInChildren<ControllerInputActionManager>(true);
            var origen = GetComponent<XROrigin>();
            m_Vineta = GetComponentInChildren<TunnelingVignetteController>(true);
            if (m_Vineta == null && prefabVineta != null && origen.Camera != null)
                m_Vineta = Instantiate(prefabVineta, origen.Camera.transform, false).GetComponentInChildren<TunnelingVignetteController>(true);

            if (m_Vineta != null)
            {
                m_Vineta.gameObject.SetActive(true);
                foreach (var p in GetComponentsInChildren<LocomotionProvider>(true))
                {
                    if (!(p is ContinuousMoveProvider || p is ContinuousTurnProvider || p is SnapTurnProvider)) continue;
                    if (m_Vineta.locomotionVignetteProviders.Exists(v => v.locomotionProvider == p)) continue;
                    var entrada = new LocomotionVignetteProvider { locomotionProvider = p, enabled = true };
                    m_Vineta.locomotionVignetteProviders.Add(entrada);
                    m_Proveedores.Add(entrada);
                }
            }
            Aplicar();
        }

        void OnEnable() { GestorPreferencias.Cambio += Aplicar; }
        void OnDisable() { GestorPreferencias.Cambio -= Aplicar; }

        void Aplicar()
        {
            if (m_Mandos == null) return;
            bool continuo = GestorPreferencias.GiroContinuo;
            foreach (var m in m_Mandos) m.smoothTurnEnabled = continuo;
            bool vineta = GestorPreferencias.ReducirMareo;
            foreach (var p in m_Proveedores) p.enabled = vineta;
        }
    }
}
