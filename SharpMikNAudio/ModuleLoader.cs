using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using SharpMik.Interfaces;
using System.Reflection;
using SharpMik.IO;
using SharpMik.Player;

namespace SharpMik
{

	/*
	 * Handles the mod loading and unloading, by the way of finding which loader should be used
	 * and asking it to load the basics of the module then do some extra setup after.
	 * 
	 * I don't see much need to change the basics of this file, the static nature of it should be fine.
	 */
	public class ModuleLoader
	{
		#region private static variables
		static bool s_UseBuiltInModuleLoaders = true;
		static List<Type> s_RegistedModuleLoader = new List<Type>();
		static bool s_HasAutoRegisted = false;
		#endregion

		#region static accessors
		static public bool UseBuiltInModuleLoaders
		{
			get { return s_UseBuiltInModuleLoaders; }
			set { s_UseBuiltInModuleLoaders = value; }
		}
		#endregion

		#region loader registration
		static public void BuildRegisteredModules()
		{
			if (!s_HasAutoRegisted && s_UseBuiltInModuleLoaders)
			{
				var list = Assembly.GetExecutingAssembly().GetTypes().Where( x => x.IsSubclassOf(typeof(IModLoader)));

				foreach (var type in list)
				{
					s_RegistedModuleLoader.Add(type);
				}
				
				s_HasAutoRegisted = false;
			}
		}

		public void RegisterModuleLoader<T>() where T: IModLoader
		{
			s_RegistedModuleLoader.Add(typeof(T));
		}
		#endregion


		#region Module Loading
		public static MikModule Load(string fileName)
		{
			//try
			{
				using (Stream stream = new FileStream(fileName, FileMode.Open,FileAccess.Read))
				{
					return Load(stream,64,0);
				}
			}
			//catch (System.Exception ex)
			{
				//throw new Exception("Failed to open " + fileName,ex);
			}
		}

		public static MikModule Load(Stream stream, int maxchan, int curious)
		{
			BuildRegisteredModules();
			MikModule mod = null;

			ModuleReader modReader = new ModuleReader(stream);
			IModLoader loader = null;

			for (int i = 0; i < s_RegistedModuleLoader.Count; i++)
			{
				modReader.Rewind();
				IModLoader tester = (IModLoader)Activator.CreateInstance(s_RegistedModuleLoader[i]);
				tester.ModuleReader = modReader;

				if (tester.Test())
				{
					loader = tester;
					tester.Cleanup();
					break;
				}

				tester.Cleanup();
			}


			if (loader != null)
			{


				int t = 0;
				mod = new MikModule();
				loader.Module = mod;

				bool loaded = false;

				munitrk track = new munitrk();
				track.UniInit();
				loader.Tracker = track;

				mod.bpmlimit = 33;
				mod.initvolume = 128;
					
				for (t = 0; t < SharpMikCommon.UF_MAXCHAN; t++)
				{
					mod.chanvol[t] = 64;
					mod.panning[t] = (ushort)((((t + 1) & 2) == 2) ? SharpMikCommon.PAN_RIGHT : SharpMikCommon.PAN_LEFT);
				}

				if (loader.Init())
				{
					modReader.Rewind();

					loaded = loader.Load(curious);

					if (loaded)
					{
						for (t = 0; t < mod.numsmp; t++)
						{
							if (mod.samples[t].inflags == 0)
							{
								mod.samples[t].inflags = mod.samples[t].flags;
							}
						}
					}
				}

				loader.Cleanup();
				track.UniCleanup();				

				if (loaded)
				{
					ML_LoadSamples(mod, modReader);

					if (!((mod.flags & SharpMikCommon.UF_PANNING) == SharpMikCommon.UF_PANNING))
					{
						for (t = 0; t < mod.numchn; t++)
						{
							mod.panning[t] = (ushort)((((t + 1) & 2) == 2)  ? SharpMikCommon.PAN_HALFRIGHT : SharpMikCommon.PAN_HALFLEFT);
						}
					}

					if (maxchan > 0)
					{
						if (!((mod.flags & SharpMikCommon.UF_NNA) == SharpMikCommon.UF_NNA) && (mod.numchn < maxchan))
						{
							maxchan = mod.numchn;
						}
						else
						{
							if ((mod.numvoices != 0) && (mod.numvoices < maxchan))
							{
								maxchan = mod.numvoices;
							}
						}

						if (maxchan < mod.numchn)
						{
							mod.flags |= SharpMikCommon.UF_NNA;
						}

						if (ModDriver.MikMod_SetNumVoices_internal(maxchan, -1))
						{
							mod = null;
							return null;
						}
					}



					SampleLoader.SL_LoadSamples();

					ModPlayer.Player_Init(mod);
				}
				else
				{
					mod = null;
					LoadFailed(loader, null);
				}
			}
			else
			{
				throw new Exception("File {0} didn't match any of the loader types");
			}

			return mod;
		}


		private static void LoadFailed(IModLoader loader, Exception ex)
		{
			if (loader != null)
			{
				if (loader.LoadError != null)
				{
					throw new Exception(loader.LoadError, ex);
				}
			}

			throw new Exception("Failed to load", ex);
		}
		#endregion


		#region Common Load Implementation
		private static bool ML_LoadSamples(MikModule of, ModuleReader modreader)
		{
			int u;

			for (u = 0; u < of.numsmp;u++ )
			{
				if (of.samples[u].length != 0)
				{
					SampleLoader.SL_RegisterSample(of.samples[u], (int)SharpMikCommon.MDTypes.MD_MUSIC, modreader);
				}
			}

			return true;
		}


		#endregion


		#region Module unloading
		public static void UnLoad(MikModule mod)
		{
			ModPlayer.Player_Exit_internal(mod);
			ML_FreeEx(mod);
		}

		static void ML_FreeEx(MikModule mf)
		{
			if (mf.samples != null)
			{
				for (ushort t = 0; t < mf.numsmp; t++)
				{
					if (mf.samples[t].length != 0)
					{
						if (ModDriver.Driver != null)
						{
							ModDriver.Driver.SampleUnload(mf.samples[t].handle);
						}						
					}
				}
			}
		}
		#endregion
	}
}
